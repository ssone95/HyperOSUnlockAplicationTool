using HOSUnlock.Configuration;
using Polly;
using Polly.Retry;
using System.Net.Sockets;

namespace HOSUnlock.Services;

/// <summary>
/// Provides shared resilience policies for network operations.
/// Retry strategy is configurable via AppConfiguration.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Creates an async retry pipeline for network operations.
    /// Retries on SocketException, HttpRequestException, and TimeoutException.
    /// </summary>
    /// <param name="operationName">Name of the operation for logging purposes.</param>
    public static ResiliencePipeline CreateAsyncRetryPipeline(string operationName)
    {
        var config = AppConfiguration.Instance;
        var maxRetries = config?.GetValidatedMaxApiRetries() ?? AppConfiguration.DefaultMaxApiRetries;
        var waitTimeMs = config?.GetValidatedApiRetryWaitTimeMs() ?? AppConfiguration.DefaultApiRetryWaitTimeMs;
        var multiplyByAttempt = config?.MultiplyApiRetryWaitTimeByAttempt ?? true;

        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<SocketException>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                MaxRetryAttempts = maxRetries,
                DelayGenerator = args =>
                {
                    var delay = multiplyByAttempt
                        ? TimeSpan.FromMilliseconds(args.AttemptNumber * waitTimeMs)
                        : TimeSpan.FromMilliseconds(waitTimeMs);
                    return ValueTask.FromResult<TimeSpan?>(delay);
                },
                OnRetry = args =>
                {
                    Logger.LogWarning(
                        "{0} failed (attempt {1}/{2}). Retrying in {3}ms. Error: {4}",
                        operationName,
                        args.AttemptNumber + 1,
                        maxRetries + 1,
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
        var config = AppConfiguration.Instance;
        var maxRetries = config?.GetValidatedMaxApiRetries() ?? AppConfiguration.DefaultMaxApiRetries;
        var waitTimeMs = config?.GetValidatedApiRetryWaitTimeMs() ?? AppConfiguration.DefaultApiRetryWaitTimeMs;
        var multiplyByAttempt = config?.MultiplyApiRetryWaitTimeByAttempt ?? true;

        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<SocketException>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                MaxRetryAttempts = maxRetries,
                DelayGenerator = args =>
                {
                    var delay = multiplyByAttempt
                        ? TimeSpan.FromMilliseconds(args.AttemptNumber * waitTimeMs)
                        : TimeSpan.FromMilliseconds(waitTimeMs);
                    return ValueTask.FromResult<TimeSpan?>(delay);
                },
                OnRetry = args =>
                {
                    Logger.LogWarning(
                        "{0} failed (attempt {1}/{2}). Retrying in {3}ms. Error: {4}",
                        operationName,
                        args.AttemptNumber + 1,
                        maxRetries + 1,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown error");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
