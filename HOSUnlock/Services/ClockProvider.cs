using GuerrillaNtp;
using HOSUnlock.Configuration;
using HOSUnlock.Constants;
using System.Collections.Concurrent;

namespace HOSUnlock.Services;

public class ClockProvider : IDisposable
{
    private NtpClient _ntpClient = null!;
    private Timer _timer = null!;
    public DateTime UtcNow
    {
        get; private set;
    }

    private readonly TimeZoneInfo _beijingTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
    private readonly TimeZoneInfo _localTimeZone = TimeZoneInfo.Local;
    private readonly TimeZoneInfo _utcTimeZone = TimeZoneInfo.Utc;


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

    private ConcurrentDictionary<int, (DateTime threshold, bool flagged)> _requestTimeThresholds = [];

    public static ClockProvider Instance { get; private set; } = null!;

    public event EventHandler<ClockThresholdExceededArgs>? OnClockThresholdExceeded;

    public event EventHandler? OnAllThresholdsReached = null!;

    private async Task<bool> WereAllThresholdsReached()
    {
        bool allReached = false;
        try
        {
            await _thresholdLoopSemaphore.WaitAsync();
            allReached = _requestTimeThresholds.Select(x => x.Value.flagged).All(x => x == true);
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

    private static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

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

            Instance._requestTimeThresholds = new ConcurrentDictionary<int, (DateTime threshold, bool flagged)>();
            Instance._requestTimeThresholds.AddOrUpdate(1, (CalculateNextThreshold(Instance._config.Token1RequestShiftMilliseconds), false), (key, oldValue) => oldValue);
            Instance._requestTimeThresholds.AddOrUpdate(2, (CalculateNextThreshold(Instance._config.Token2RequestShiftMilliseconds), false), (key, oldValue) => oldValue);
            Instance._requestTimeThresholds.AddOrUpdate(3, (CalculateNextThreshold(Instance._config.Token3RequestShiftMilliseconds), false), (key, oldValue) => oldValue);
            Instance._requestTimeThresholds.AddOrUpdate(4, (CalculateNextThreshold(Instance._config.Token4RequestShiftMilliseconds), false), (key, oldValue) => oldValue);

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

    private readonly SemaphoreSlim _thresholdLoopSemaphore = new SemaphoreSlim(1, 1);


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
            ProcessThreshold(1);
            ProcessThreshold(2);
            ProcessThreshold(3);
            ProcessThreshold(4);

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

    public async Task<Dictionary<int, (DateTime beijing, DateTime utc, DateTime local)>> GetThresholdSnapshots()
    {
        var result = new Dictionary<int, (DateTime beijing, DateTime utc, DateTime local)>();

        var beijingTimezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
        var localTimezone = TimeZoneInfo.Local;

        foreach (var kvp in _requestTimeThresholds)
        {
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(kvp.Value.threshold, beijingTimezone);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, localTimezone);

            result.Add(kvp.Key, (kvp.Value.threshold, utcTime, localTime));
        }
        return result;
    }

    private void ProcessThreshold(int thresholdIndex)
    {
        if (!_requestTimeThresholds.TryGetValue(thresholdIndex, out var threshold))
            return;

        if ((BeijingNow >= threshold.threshold
            /*|| UtcNow.Second == 0 && thresholdIndex == 1*/)
            && !threshold.flagged) // Safety check for 16:43:00 Beijing Time
        {
            // _requestTimeThresholds.TryRemove(thresholdIndex, out _);
            _requestTimeThresholds.AddOrUpdate(thresholdIndex, (threshold.threshold, true), (key, oldValue) => (threshold.threshold, true));

            Logger.LogInfo("Clock threshold exceeded for Token{0} at Beijing Time: {1}",
                thresholdIndex,
                BeijingNow.ToString("yyyy-MM-dd HH:mm:ss"));

            OnClockThresholdExceeded?.Invoke(this, new ClockThresholdExceededArgs
            {
                ThresholdIndex = thresholdIndex,
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

        var beijingThresholdDt = TimeZoneInfo.ConvertTimeFromUtc(nextThresholdUtc, TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai"));
        if (beijingThresholdDt <= (utcNow.AddHours(8)))
        {
            nextThresholdUtc = nextThresholdUtc.AddDays(1);
            beijingThresholdDt = TimeZoneInfo.ConvertTimeFromUtc(nextThresholdUtc, TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai"));
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
        public int ThresholdIndex { get; set; }
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