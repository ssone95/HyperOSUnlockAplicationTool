namespace HOSUnlock.Services;

/// <summary>
/// Interface for Logger to enable mocking in tests.
/// </summary>
public interface ILogger : IDisposable
{
    void Log(LogLevel level, string message, Exception? ex = null, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogInfo(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, Exception? ex = null, params object[] args);
}

/// <summary>
/// Log level enumeration.
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public sealed class Logger : ILogger
{
    private readonly FileStream _logFileStream;
    private readonly StreamWriter _logStreamWriter;
    private readonly string _prefix;
    private readonly bool _logToConsoleToo;
    private readonly Lock _writeLock = new();
    private bool _disposed;

    private Logger(string prefix, bool logToConsoleToo)
    {
        _prefix = prefix;
        _logToConsoleToo = logToConsoleToo;

        var logPath = InitializeLogsFolder();
        var logFilePath = GetLogFilePath(logPath);

        _logFileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _logStreamWriter = new StreamWriter(_logFileStream) { AutoFlush = true };
    }

    public static Logger? Instance { get; private set; }

    public static void InitializeLogger(string prefix = "UI", bool logToConsoleToo = false)
    {
        Instance ??= new Logger(prefix, logToConsoleToo);
    }

    public static void DisposeLogger()
    {
        Instance?.Dispose();
        Instance = null;
    }

    private string GetLogFilePath(string logPath)
    {
        var now = DateTime.Now;
        var logFileName = $"log_{_prefix}_{now:yyyyMMdd_HHmm}.txt";
        return Path.Combine(logPath, logFileName);
    }

    private static string InitializeLogsFolder()
    {
        var logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        if (!Directory.Exists(logsDirectory))
        {
            Directory.CreateDirectory(logsDirectory);
        }

        return logsDirectory;
    }

    // Static methods for convenience (delegate to instance)
    public static void Log(LogLevel level, string message, Exception? ex = null, params object[] args)
    {
        Instance?.LogInstance(level, message, ex, args);
    }

    public static void LogDebug(string message, params object[] args)
    {
        if (!string.IsNullOrWhiteSpace(message))
            Log(LogLevel.Debug, message, null, args);
    }

    public static void LogInfo(string message, params object[] args)
    {
        if (!string.IsNullOrWhiteSpace(message))
            Log(LogLevel.Info, message, null, args);
    }

    public static void LogWarning(string message, params object[] args)
    {
        Log(LogLevel.Warning, message, null, args);
    }

    public static void LogError(string message, Exception? ex = null, params object[] args)
    {
        Log(LogLevel.Error, message, ex, args);
    }

    // Instance methods (ILogger implementation)
    void ILogger.Log(LogLevel level, string message, Exception? ex, params object[] args)
    {
        LogInstance(level, message, ex, args);
    }

    void ILogger.LogDebug(string message, params object[] args)
    {
        if (!string.IsNullOrWhiteSpace(message))
            LogInstance(LogLevel.Debug, message, null, args);
    }

    void ILogger.LogInfo(string message, params object[] args)
    {
        if (!string.IsNullOrWhiteSpace(message))
            LogInstance(LogLevel.Info, message, null, args);
    }

    void ILogger.LogWarning(string message, params object[] args)
    {
        LogInstance(LogLevel.Warning, message, null, args);
    }

    void ILogger.LogError(string message, Exception? ex, params object[] args)
    {
        LogInstance(LogLevel.Error, message, ex, args);
    }

    private void LogInstance(LogLevel level, string message, Exception? ex, params object[] args)
    {
        var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
        var logMessage = $"[{FormatLogLevel(level)}] [{_prefix}] {formattedMessage}";

        if (ex is not null)
        {
            logMessage += $"\nException: {ex}";
        }

        WriteLog(logMessage);
    }

    private void WriteLog(string message)
    {
        if (_disposed)
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"{timestamp} {message}";

        lock (_writeLock)
        {
            if (_disposed)
                return;

            _logStreamWriter.WriteLine(logMessage);
        }

        if (_logToConsoleToo)
            Console.WriteLine(logMessage);
    }

    private static string FormatLogLevel(LogLevel level) => level switch
    {
        LogLevel.Debug => "DBG",
        LogLevel.Info => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        _ => "N/A"
    };

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _logStreamWriter.Flush();
        _logStreamWriter.Dispose();
        _logFileStream.Dispose();
    }
}
