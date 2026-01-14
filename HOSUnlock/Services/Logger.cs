using System;

namespace HOSUnlock.Services;

public sealed class Logger : IDisposable
{
    private readonly FileStream _logFileStream;
    private readonly StreamWriter _logStreamWriter;
    private readonly string _prefix;
    private readonly bool _logToConsoleToo;
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

    public static void Log(LogLevel level, string message, Exception? ex = null, params object[] args)
    {
        if (Instance is null)
            return;

        var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
        var logMessage = $"[{FormatLogLevel(level)}] [{Instance._prefix}] {formattedMessage}";

        if (ex is not null)
        {
            logMessage += $"\nException: {ex}";
        }

        Instance.WriteLog(logMessage);
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

    private void WriteLog(string message)
    {
        if (_disposed)
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"{timestamp} {message}";

        _logStreamWriter.WriteLine(logMessage);

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

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}
