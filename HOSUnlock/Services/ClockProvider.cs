//#define DBG

using GuerrillaNtp;
using HOSUnlock.Configuration;
using HOSUnlock.Constants;
using HOSUnlock.Models.Common;
using System.Collections.Concurrent;

namespace HOSUnlock.Services;

public class ClockProvider : IDisposable
{
    private const string ShanghaiTimeZoneString = "Asia/Shanghai";

    private NtpClient _ntpClient = null!;
    private Timer _timer = null!;
    public DateTime UtcNow
    {
        get; private set;
    }

    private readonly TimeZoneInfo _beijingTimeZone = TimeZoneInfo.FindSystemTimeZoneById(ShanghaiTimeZoneString);
    private readonly TimeZoneInfo _localTimeZone = TimeZoneInfo.Local;


    public DateTime BeijingNow
    {
        get
        {
            return TimeZoneInfo.ConvertTimeFromUtc(UtcNow, _beijingTimeZone);
        }
    }

    public DateTime LocalNow
    {
        get
        {
            return TimeZoneInfo.ConvertTimeFromUtc(UtcNow, _localTimeZone);
        }
    }

    private AppConfiguration _config = AppConfiguration.Instance;

    private ConcurrentDictionary<TokenShiftDefinition, (DateTime threshold, bool flagged)> _requestTimeThresholds = [];

    public static ClockProvider Instance { get; private set; } = null!;

    public event EventHandler<ClockThresholdExceededArgs>? OnClockThresholdExceeded;

    public event EventHandler? OnAllThresholdsReached = null!;

    public async Task<bool> WereAllThresholdsReached()
    {
        bool allReached = false;
        try
        {
            await _thresholdLoopSemaphore.WaitAsync();
            allReached = _requestTimeThresholds.Select(x => x.Value.flagged).All(x => x);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error checking if all thresholds were reached.", ex);
        }
        finally
        {
            _thresholdLoopSemaphore.Release();
        }
        return allReached;
    }

    public static void DisposeInstance()
    {
        Instance.Dispose();
    }

    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public static async Task Initialize()
    {
        await _initLock.WaitAsync();
        try
        {
            if (Instance != null)
                return;

            Instance = new()
            {
                _ntpClient = new NtpClient(NtpConstants.NtpServers[0], TimeSpan.FromSeconds(5))
                {
                    Timeout = TimeSpan.FromSeconds(5)
                }
            };

            var originalUtcTime = Instance.GetUtcTime();
            Instance.UtcNow = new DateTime(
                originalUtcTime.Year,
                originalUtcTime.Month,
                originalUtcTime.Day,
                originalUtcTime.Hour,
                originalUtcTime.Minute,
                originalUtcTime.Second,
                originalUtcTime.Millisecond,
                DateTimeKind.Utc);

            Instance._requestTimeThresholds = new ConcurrentDictionary<TokenShiftDefinition, (DateTime threshold, bool flagged)>();
            foreach (var shiftPair in Instance
                ._config
                .Tokens
                .SelectMany(tokenInfo => Instance
                    ._config
                    .TokenShifts
                    .Select((shift, index) => new TokenShiftDefinition(tokenInfo.Index, tokenInfo.Token, index + 1, shift))))
            {
                Instance._requestTimeThresholds.AddOrUpdate(shiftPair, (CalculateNextThreshold(shiftPair.ShiftMilliseconds), false), (key, oldValue) => oldValue);
            }

            Logger.LogInfo("Initial NTP time obtained: {0} (Beijing Time: {1})",
                Instance.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                Instance.BeijingNow.ToString("yyyy-MM-dd HH:mm:ss"));

            Instance._timer = new Timer(
                async state => await Instance.ProcessThresholds(),
                state: null,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(NtpConstants.NtpRefreshIntervalMilliseconds));
        }
        finally
        {
            _initLock.Release();
        }
    }

    private readonly SemaphoreSlim _thresholdLoopSemaphore = new(1, 1);


    private async Task ExecuteOnDictionarySync(Action executeAfter)
    {
        try
        {
            await _thresholdLoopSemaphore.WaitAsync();
            var utcNow = DateTime.UtcNow;
            var diff = utcNow - UtcNow;
            UtcNow = UtcNow.Add(diff);
            executeAfter();
        }
        finally
        {
            _thresholdLoopSemaphore.Release();
        }
    }

    private async Task ProcessThresholds()
    {
        await ExecuteOnDictionarySync(async () =>
        {
            foreach (var threshold in _requestTimeThresholds.Keys)
            {
                ProcessThreshold(threshold);
            }

            var allReached = _requestTimeThresholds.Select(x => x.Value.flagged).All(x => x);
            if (allReached && _timer.Change(Timeout.Infinite, Timeout.Infinite))
            {
                await Task.Delay(1000);
                OnAllThresholdsReached?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    public async Task<(DateTime localTime, DateTime utcTime, DateTime beijingTime)> GetCurrentTimes()
    {
        return (LocalNow, UtcNow, BeijingNow);
    }

    public async Task<Dictionary<TokenShiftDefinition, (DateTime beijing, DateTime utc, DateTime local)>> GetThresholdSnapshots()
    {
        var result = new Dictionary<TokenShiftDefinition, (DateTime beijing, DateTime utc, DateTime local)>();

        var beijingTimezone = TimeZoneInfo.FindSystemTimeZoneById(ShanghaiTimeZoneString);
        var localTimezone = TimeZoneInfo.Local;

        foreach (var kvp in _requestTimeThresholds)
        {
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(kvp.Value.threshold, beijingTimezone);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, localTimezone);

            result.Add(kvp.Key, (kvp.Value.threshold, utcTime, localTime));
        }
        return result;
    }

    private void ProcessThreshold(TokenShiftDefinition tokenShiftDefinition)
    {
        if (!_requestTimeThresholds.TryGetValue(tokenShiftDefinition, out var threshold))
            return;
#if DBG
        if ((BeijingNow >= threshold.threshold || BeijingNow.Second == 0) && !threshold.flagged)
#else
        if (BeijingNow >= threshold.threshold && !threshold.flagged)
#endif
        {
            _requestTimeThresholds.AddOrUpdate(tokenShiftDefinition, (threshold.threshold, true), (key, oldValue) => (threshold.threshold, true));

            Logger.LogDebug($"Clock threshold exceeded: Token #{tokenShiftDefinition.TokenIndex} Shift #{tokenShiftDefinition.ShiftIndex}\n\tBeijing Time: {BeijingNow:yyyy-MM-dd HH:mm:ss.fff}\n\tUTC Time: {UtcNow:yyyy-MM-dd HH:mm:ss.fff}\n\tLocal Time: {LocalNow:yyyy-MM-dd HH:mm:ss.fff}");

            OnClockThresholdExceeded?.Invoke(this, new ClockThresholdExceededArgs
            {
                TokenShiftDetails = tokenShiftDefinition,
                UtcTime = UtcNow,
                BeijingTime = BeijingNow
            });
        }
    }

    private static DateTime CalculateNextThreshold(int shiftMilliseconds)
    {
        var utcNow = Instance.UtcNow;
        var nextThresholdUtc = new DateTime(
            utcNow.Year,
            utcNow.Month,
            utcNow.Day,
            16,
            0, 0, 0,
            DateTimeKind.Utc);

        var beijingThresholdDt = TimeZoneInfo.ConvertTimeFromUtc(nextThresholdUtc, TimeZoneInfo.FindSystemTimeZoneById(ShanghaiTimeZoneString));
        if (beijingThresholdDt <= (utcNow.AddHours(8)))
        {
            nextThresholdUtc = nextThresholdUtc.AddDays(1);
            beijingThresholdDt = TimeZoneInfo.ConvertTimeFromUtc(nextThresholdUtc, TimeZoneInfo.FindSystemTimeZoneById(ShanghaiTimeZoneString));
        }

        var thresholdDt = beijingThresholdDt.AddMilliseconds(-shiftMilliseconds);
        return thresholdDt;
    }

    private DateTime GetUtcTime()
    {
        var response = _ntpClient.Query();
        return response.UtcNow.UtcDateTime;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public class ClockThresholdExceededArgs
    {
        public TokenShiftDefinition TokenShiftDetails { get; set; }
        public DateTime UtcTime { get; set; }
        public DateTime BeijingTime { get; set; }
        public DateTime LocalTime
        {
            get
            {
                return TimeZoneInfo.ConvertTimeFromUtc(UtcTime, TimeZoneInfo.Local);
            }
        }
    }
}