using System.Net;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Extensions.Http;

namespace lockhaven_backend.Infrastructure.Http;

/// <summary>
/// Resilience policies for HashiCorp Vault Transit HTTP calls (per-try timeout, retries, circuit breaker).
/// </summary>
public static class VaultTransitHttpPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> CreatePerTryTimeoutPolicy(IConfiguration config)
    {
        var seconds = config.GetValue("Vault:PerTryTimeoutSeconds", 30);
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(seconds));
    }

    public static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(IConfiguration config)
    {
        var maxRetries = config.GetValue("Vault:MaxRetries", 3);
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                maxRetries,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)));
    }

    public static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy(IConfiguration config)
    {
        var failuresBeforeBreak = config.GetValue("Vault:CircuitBreakerFailuresBeforeBreak", 5);
        var breakSeconds = config.GetValue("Vault:CircuitBreakerBreakSeconds", 30);
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                failuresBeforeBreak,
                TimeSpan.FromSeconds(breakSeconds));
    }
}
