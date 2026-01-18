namespace HOSUnlock.Configuration;

/// <summary>
/// Holds command-line argument values that can override appsettings.json.
/// Null values indicate the argument was not provided (use config file value).
/// </summary>
public sealed class CommandLineOptions
{
    /// <summary>
    /// --headless: Run in headless mode without TUI.
    /// </summary>
    public bool? HeadlessMode { get; set; }

    /// <summary>
    /// --auto-run: Automatically start monitoring without user interaction.
    /// </summary>
    public bool? AutoRunOnStart { get; set; }

    /// <summary>
    /// --max-retries &lt;n&gt;: Maximum number of auto-retry attempts (1-365).
    /// </summary>
    public int? MaxAutoRetries { get; set; }

    /// <summary>
    /// --max-api-retries &lt;n&gt;: Number of Polly retries for HTTP/NTP requests (0-10).
    /// </summary>
    public int? MaxApiRetries { get; set; }

    /// <summary>
    /// --api-retry-wait &lt;ms&gt;: Base wait time in milliseconds between retries (1-1000).
    /// </summary>
    public int? ApiRetryWaitTimeMs { get; set; }

    /// <summary>
    /// --fixed-retry-wait: Use fixed wait time instead of multiplying by attempt number.
    /// </summary>
    public bool? MultiplyApiRetryWaitTimeByAttempt { get; set; }

    /// <summary>
    /// Applies command-line overrides to the loaded configuration.
    /// </summary>
    public void ApplyTo(AppConfiguration config)
    {
        if (HeadlessMode.HasValue)
            config.HeadlessMode = HeadlessMode.Value;

        if (AutoRunOnStart.HasValue)
            config.AutoRunOnStart = AutoRunOnStart.Value;

        // Note: MaxAutoRetries, MaxApiRetries, ApiRetryWaitTimeMs, and MultiplyApiRetryWaitTimeByAttempt
        // are init-only in AppConfiguration, so we need to handle them differently.
        // They will be applied via a new method in AppConfiguration.
    }

    /// <summary>
    /// Returns true if headless mode should be used (from args).
    /// </summary>
    public bool ShouldRunHeadless => HeadlessMode == true;
}
