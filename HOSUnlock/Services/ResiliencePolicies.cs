using Polly;
using Polly.Retry;
using System.Net.Sockets;

namespace HOSUnlock.Services;

/// <summary>
/// Provides shared resilience policies for network operations.
/// Retry strategy: 2 additional attempts with delays of 0ms, 100ms (calculated as (attempt - 1) * 100ms).
/// </summary>
public static class ResiliencePolicies
{
    private const int MaxRetryAttempts = 2;

    /// <summary>
    /// Creates an async retry pipeline for network operations.
    /// Retries on SocketException, HttpRequestException, and TimeoutException.
    /// </summary>
    /// <param name="operationName">Name of the operation for logging purposes.</param>
    public static ResiliencePipeline CreateAsyncRetryPipeline(string operationName)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<SocketException>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                MaxRetryAttempts = MaxRetryAttempts,
                DelayGenerator = args =>
                {
                    var delay = TimeSpan.FromMilliseconds((args.AttemptNumber) * 100);
                    return ValueTask.FromResult<TimeSpan?>(delay);
                },
                OnRetry = args =>
                {
                    Logger.LogWarning(
                        "{0} failed (attempt {1}/{2}). Retrying in {3}ms. Error: {4}",
                        operationName,
                        args.AttemptNumber + 1,
                        MaxRetryAttempts + 1,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown error");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Creates a sync retry pipeline for network operations.
    /// Retries on SocketException, HttpRequestException, and TimeoutException.
    /// </summary>
    /// <param name="operationName">Name of the operation for logging purposes.</param>
    public static ResiliencePipeline CreateSyncRetryPipeline(string operationName)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<SocketException>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                MaxRetryAttempts = MaxRetryAttempts,
                DelayGenerator = args =>
                {
                    var delay = TimeSpan.FromMilliseconds((args.AttemptNumber) * 100);
                    return ValueTask.FromResult<TimeSpan?>(delay);
                },
                OnRetry = args =>
                {
                    Logger.LogWarning(
                        "{0} failed (attempt {1}/{2}). Retrying in {3}ms. Error: {4}",
                        operationName,
                        args.AttemptNumber + 1,
                        MaxRetryAttempts + 1,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown error");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
