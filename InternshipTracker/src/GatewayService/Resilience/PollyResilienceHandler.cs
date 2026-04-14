using Polly;
namespace GatewayService.Resilience;
internal sealed class PollyResilienceHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;
    public PollyResilienceHandler(
        ResiliencePipeline<HttpResponseMessage> pipeline,
        HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _pipeline = pipeline;
    }
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => _pipeline.ExecuteAsync(
                ct => new ValueTask<HttpResponseMessage>(base.SendAsync(request, ct)),
                cancellationToken)
            .AsTask();
}
