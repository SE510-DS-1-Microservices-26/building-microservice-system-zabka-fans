namespace GatewayService.Resilience;

public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public RetryOptions Retry { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public int TimeoutSeconds { get; set; } = 10;
}

public sealed class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public double BaseDelaySeconds { get; set; } = 1;
}

public sealed class CircuitBreakerOptions
{
    public int MinimumThroughput { get; set; } = 5;
    public double FailureRatio { get; set; } = 0.5;
    public int BreakDurationSeconds { get; set; } = 30;
    public int SamplingDurationSeconds { get; set; } = 60;
}

