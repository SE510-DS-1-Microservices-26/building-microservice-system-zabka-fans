using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Yarp.ReverseProxy.Forwarder;
namespace GatewayService.Resilience;
public sealed class ResilientForwarderHttpClientFactory : IForwarderHttpClientFactory
{
    private readonly ResilienceOptions _options;
    private readonly ILogger<ResilientForwarderHttpClientFactory> _logger;
    private readonly ConcurrentDictionary<string, ResiliencePipeline<HttpResponseMessage>> _pipelines = new();
    public ResilientForwarderHttpClientFactory(
        IOptions<ResilienceOptions> options,
        ILogger<ResilientForwarderHttpClientFactory> logger)
    {
        _options = options.Value;
        _logger  = logger;
    }
    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
    {
        if (context.OldClient is not null && context.NewConfig == context.OldConfig)
            return context.OldClient;
        var pipeline = _pipelines.GetOrAdd(context.ClusterId, BuildPipeline);
        var socketsHandler = new SocketsHttpHandler
        {
            UseProxy                   = false,
            AllowAutoRedirect          = false,
            AutomaticDecompression     = DecompressionMethods.None,
            UseCookies                 = false,
            EnableMultipleHttp2Connections = true,
            ActivityHeadersPropagator  = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout             = TimeSpan.FromSeconds(15),
        };
        return new HttpMessageInvoker(new PollyResilienceHandler(pipeline, socketsHandler));
    }
    // Pipeline order (outer to inner):  CircuitBreaker -> Retry -> Timeout -> Handler
    private ResiliencePipeline<HttpResponseMessage> BuildPipeline(string clusterId)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                SamplingDuration  = TimeSpan.FromSeconds(_options.CircuitBreaker.SamplingDurationSeconds),
                FailureRatio      = _options.CircuitBreaker.FailureRatio,
                MinimumThroughput = _options.CircuitBreaker.MinimumThroughput,
                BreakDuration     = TimeSpan.FromSeconds(_options.CircuitBreaker.BreakDurationSeconds),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(r => r.StatusCode is
                        HttpStatusCode.BadGateway or
                        HttpStatusCode.ServiceUnavailable or
                        HttpStatusCode.GatewayTimeout),
                OnOpened = args =>
                {
                    _logger.LogError(
                        "Circuit breaker OPENED for cluster {ClusterId}. Pausing for {Duration}s",
                        clusterId, args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    _logger.LogInformation("Circuit breaker CLOSED for cluster {ClusterId}", clusterId);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = _ =>
                {
                    _logger.LogInformation(
                        "Circuit breaker HALF-OPEN for cluster {ClusterId}, probing...", clusterId);
                    return ValueTask.CompletedTask;
                },
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = _options.Retry.MaxAttempts,
                Delay            = TimeSpan.FromSeconds(_options.Retry.BaseDelaySeconds),
                BackoffType      = DelayBackoffType.Exponential,
                UseJitter        = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => r.StatusCode is
                        HttpStatusCode.BadGateway or
                        HttpStatusCode.ServiceUnavailable or
                        HttpStatusCode.GatewayTimeout),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retrying request to cluster {ClusterId}: attempt {Attempt}/{Max}, delay {Delay}ms",
                        clusterId, args.AttemptNumber + 1,
                        _options.Retry.MaxAttempts, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                },
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds),
                OnTimeout = _ =>
                {
                    _logger.LogWarning(
                        "Request to cluster {ClusterId} timed out after {Timeout}s",
                        clusterId, _options.TimeoutSeconds);
                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }
}
