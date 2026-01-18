using GuerrillaNtp;
using HOSUnlock.Configuration;
using HOSUnlock.Constants;
using HOSUnlock.Models.Common;
using Polly;
using System.Collections.Concurrent;

namespace HOSUnlock.Services;

/// <summary>
/// Interface for ClockProvider to enable mocking in tests.
/// </summary>
public interface IClockProvider : IDisposable
{
    DateTime UtcNow { get; }
    DateTime BeijingNow { get; }
    DateTime LocalNow { get; }
    int AttemptCount { get; }
    int RemainingAttempts { get; }
    bool CanRetry { get; }
    int MaxRetryAttempts { get; }

    event EventHandler<ClockProvider.ClockThresholdExceededEventArgs>? OnClockThresholdExceeded;
    event EventHandler? OnAllThresholdsReached;
    event EventHandler? OnMaxRetriesReached;

    Task<bool> WereAllThresholdsReachedAsync();
    (DateTime LocalTime, DateTime UtcTime, DateTime BeijingTime) GetCurrentTimes();
    Dictionary<TokenShiftDefinition, (DateTime Beijing, DateTime Utc, DateTime Local)> GetThresholdSnapshots();
    Task<bool> ResetAndRestartAsync();
    void Stop();
    void Resume();
}

public sealed class ClockProvider : IClockProvider
{
    private const string ShanghaiTimeZoneId = "Asia/Shanghai";
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    private readonly int _maxRetryAttempts;
    private readonly NtpClient _ntpClient;
    private readonly TimeZoneInfo _beijingTimeZone;
    private readonly TimeZoneInfo _localTimeZone;
    private readonly AppConfiguration _config;
    private readonly SemaphoreSlim _thresholdLoopSemaphore = new(1, 1);
    private readonly ConcurrentDictionary<TokenShiftDefinition, (DateTime Threshold, bool Flagged)> _requestTimeThresholds;
    private readonly ResiliencePipeline _ntpRetryPipeline;

    private Timer? _timer;
    private bool _disposed;
    private int _attemptCount;

    public DateTime UtcNow { get; private set; }

    public DateTime BeijingNow => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, _beijingTimeZone);

    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, _localTimeZone);

    public static ClockProvider? Instance { get; private set; }

    public int AttemptCount => _attemptCount;

    public int RemainingAttempts => _maxRetryAttempts - _attemptCount;

    public bool CanRetry => _attemptCount < _maxRetryAttempts;

    public int MaxRetryAttempts => _maxRetryAttempts;

    public event EventHandler<ClockThresholdExceededEventArgs>? OnClockThresholdExceeded;
    public event EventHandler? OnAllThresholdsReached;
    public event EventHandler? OnMaxRetriesReached;

    private ClockProvider(AppConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _maxRetryAttempts = config.GetValidatedMaxAutoRetries();
        _beijingTimeZone = TimeZoneInfo.FindSystemTimeZoneById(ShanghaiTimeZoneId);
        _localTimeZone = TimeZoneInfo.Local;
        _requestTimeThresholds = new ConcurrentDictionary<TokenShiftDefinition, (DateTime, bool)>();

        _ntpClient = new NtpClient(NtpConstants.NtpServers[0], TimeSpan.FromSeconds(5))
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        _ntpRetryPipeline = ResiliencePolicies.CreateSyncRetryPipeline("NTP Query");
    }

    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public static async Task InitializeAsync()
    {
        await _initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (Instance is not null)
                return;

            var config = AppConfiguration.Instance
                ?? throw new InvalidOperationException("AppConfiguration must be initialized before ClockProvider.");

            var instance = new ClockProvider(config);

            var originalUtcTime = instance.GetUtcTime();
            instance.UtcNow = new DateTime(
                originalUtcTime.Year,
                originalUtcTime.Month,
                originalUtcTime.Day,
                originalUtcTime.Hour,
                originalUtcTime.Minute,
                originalUtcTime.Second,
                originalUtcTime.Millisecond,
                DateTimeKind.Utc);

            instance.InitializeThresholds();

            Logger.LogInfo(
                "Initial NTP time obtained: {0} (Beijing Time: {1})",
                instance.UtcNow.ToString(DateTimeFormat),
                instance.BeijingNow.ToString(DateTimeFormat));

            instance._timer = new Timer(
                instance.ProcessThresholdsCallback,
                state: null,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(NtpConstants.NtpRefreshIntervalMilliseconds));

            Instance = instance;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private void InitializeThresholds()
    {
        var thresholds = _config.Tokens
            .SelectMany(tokenInfo => _config.TokenShifts
                .Select((shift, index) => new TokenShiftDefinition(
                    tokenInfo.Index,
                    tokenInfo.Token,
                    index + 1,
                    shift)));

        foreach (var threshold in thresholds)
        {
            var thresholdTime = CalculateNextThreshold(threshold.ShiftMilliseconds);
            _requestTimeThresholds.TryAdd(threshold, (thresholdTime, false));
        }
    }

    public static void DisposeInstance()
    {
        Instance?.Dispose();
        Instance = null;
    }

    public async Task<bool> WereAllThresholdsReachedAsync()
    {
        await _thresholdLoopSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            return _requestTimeThresholds.Values.All(x => x.Flagged);
        }
        finally
        {
            _thresholdLoopSemaphore.Release();
        }
    }

    private async void ProcessThresholdsCallback(object? state)
    {
        try
        {
            await ProcessThresholdsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError("Error in ClockProvider threshold processing: {0}", ex, ex.Message);
        }
    }

    private async Task ProcessThresholdsAsync()
    {
        await ExecuteWithSemaphoreAsync(async () =>
        {
            UpdateCurrentTime();

            foreach (var threshold in _requestTimeThresholds.Keys)
            {
                ProcessThreshold(threshold);
            }

            var allReached = _requestTimeThresholds.Values.All(x => x.Flagged);
            if (allReached && _timer?.Change(Timeout.Infinite, Timeout.Infinite) == true)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                OnAllThresholdsReached?.Invoke(this, EventArgs.Empty);
            }
        }).ConfigureAwait(false);
    }

    private async Task ExecuteWithSemaphoreAsync(Func<Task> action)
    {
        await _thresholdLoopSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _thresholdLoopSemaphore.Release();
        }
    }

    private void UpdateCurrentTime()
    {
        var utcNow = DateTime.UtcNow;
        var diff = utcNow - UtcNow;
        UtcNow = UtcNow.Add(diff);
    }

    public (DateTime LocalTime, DateTime UtcTime, DateTime BeijingTime) GetCurrentTimes()
        => (LocalNow, UtcNow, BeijingNow);

    public Dictionary<TokenShiftDefinition, (DateTime Beijing, DateTime Utc, DateTime Local)> GetThresholdSnapshots()
    {
        var result = new Dictionary<TokenShiftDefinition, (DateTime, DateTime, DateTime)>();

        foreach (var kvp in _requestTimeThresholds)
        {
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(kvp.Value.Threshold, _beijingTimeZone);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, _localTimeZone);
            result.Add(kvp.Key, (kvp.Value.Threshold, utcTime, localTime));
        }

        return result;
    }

    private void ProcessThreshold(TokenShiftDefinition tokenShiftDefinition)
    {
        if (!_requestTimeThresholds.TryGetValue(tokenShiftDefinition, out var threshold))
            return;

        var shouldTrigger = BeijingNow >= threshold.Threshold && !threshold.Flagged;

        if (!shouldTrigger)
            return;

        _requestTimeThresholds[tokenShiftDefinition] = (threshold.Threshold, true);

        Logger.LogDebug(
            $"Clock threshold exceeded: Token #{tokenShiftDefinition.TokenIndex} Shift #{tokenShiftDefinition.ShiftIndex}\n" +
            $"\tBeijing Time: {BeijingNow:yyyy-MM-dd HH:mm:ss.fff}\n" +
            $"\tUTC Time: {UtcNow:yyyy-MM-dd HH:mm:ss.fff}\n" +
            $"\tLocal Time: {LocalNow:yyyy-MM-dd HH:mm:ss.fff}");

        OnClockThresholdExceeded?.Invoke(this, new ClockThresholdExceededEventArgs(
            tokenShiftDefinition,
            UtcNow,
            BeijingNow));
    }

    private DateTime CalculateNextThreshold(int shiftMilliseconds)
    {
        var nextThresholdUtc = new DateTime(
            UtcNow.Year,
            UtcNow.Month,
            UtcNow.Day,
            16, 0, 0, 0,
            DateTimeKind.Utc);

        var beijingThresholdDt = TimeZoneInfo.ConvertTimeFromUtc(nextThresholdUtc, _beijingTimeZone);

        if (beijingThresholdDt <= UtcNow.AddHours(8))
        {
            nextThresholdUtc = nextThresholdUtc.AddDays(1);
            beijingThresholdDt = TimeZoneInfo.ConvertTimeFromUtc(nextThresholdUtc, _beijingTimeZone);
        }

        return beijingThresholdDt.AddMilliseconds(-shiftMilliseconds);
    }

    private DateTime GetUtcTime()
    {
        var response = _ntpRetryPipeline.Execute(() => _ntpClient.Query());
        return response.UtcNow.UtcDateTime;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _timer?.Dispose();
        _thresholdLoopSemaphore.Dispose();
    }

    public async Task<bool> ResetAndRestartAsync()
    {
        await _thresholdLoopSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            _attemptCount++;

            if (_attemptCount >= _maxRetryAttempts)
            {
                Logger.LogWarning("Maximum retry attempts ({0}) reached. Please restart the application.", _maxRetryAttempts);
                OnMaxRetriesReached?.Invoke(this, EventArgs.Empty);
                return false;
            }

            Logger.LogInfo("Resetting for attempt {0} of {1}...", _attemptCount + 1, _maxRetryAttempts);

            // Update current time from NTP
            var originalUtcTime = GetUtcTime();
            UtcNow = new DateTime(
                originalUtcTime.Year,
                originalUtcTime.Month,
                originalUtcTime.Day,
                originalUtcTime.Hour,
                originalUtcTime.Minute,
                originalUtcTime.Second,
                originalUtcTime.Millisecond,
                DateTimeKind.Utc);

            // Clear and reinitialize thresholds
            _requestTimeThresholds.Clear();
            InitializeThresholds();

            Logger.LogInfo(
                "Thresholds reset. NTP time: {0} (Beijing Time: {1})",
                UtcNow.ToString(DateTimeFormat),
                BeijingNow.ToString(DateTimeFormat));

            // Restart the timer
            _timer?.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(NtpConstants.NtpRefreshIntervalMilliseconds));

            return true;
        }
        finally
        {
            _thresholdLoopSemaphore.Release();
        }
    }

    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        Logger.LogInfo("ClockProvider timer stopped.");
    }

    public void Resume()
    {
        if (!CanRetry)
        {
            Logger.LogWarning("Cannot resume: maximum retry attempts reached.");
            return;
        }

        _timer?.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(NtpConstants.NtpRefreshIntervalMilliseconds));
        Logger.LogInfo("ClockProvider timer resumed.");
    }

    public sealed class ClockThresholdExceededEventArgs(
        TokenShiftDefinition tokenShiftDetails,
        DateTime utcTime,
        DateTime beijingTime) : EventArgs
    {
        public TokenShiftDefinition TokenShiftDetails { get; } = tokenShiftDetails;
        public DateTime UtcTime { get; } = utcTime;
        public DateTime BeijingTime { get; } = beijingTime;
        public DateTime LocalTime => TimeZoneInfo.ConvertTimeFromUtc(UtcTime, TimeZoneInfo.Local);
    }
}