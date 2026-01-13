using System;

namespace HOSUnlock.Services;

public class Logger : IDisposable
{
    public FileStream logFileStream = null!;
    public StreamWriter logStreamWriter = null!;

    private readonly string _prefix;
    private readonly bool _logToConsoleToo = false;

    private Logger(string prefix = "UI", bool logToConsoleToo = false)
    {
        _prefix = prefix;
        _logToConsoleToo = logToConsoleToo;
        var logPath = InitializeLogsFolder();
        var logFilePath = GetLogFilePath(logPath);
        InitializeLogFileStream(logFilePath);
    }

    private string GetLogFilePath(string logPath)
    {
        var now = DateTime.Now;
        var logFileName = $"log_{_prefix}_{now:yyyyMMdd_HHmm}.txt";
        var logFilePath = System.IO.Path.Combine(logPath, logFileName);
        return logFilePath;
    }

    private void InitializeLogFileStream(string logFilePath)
    {
        logFileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        logStreamWriter = new StreamWriter(logFileStream)
        {
            AutoFlush = true,
        };
    }

    private void DeinitializeLogStream()
    {
        logStreamWriter?.Flush();
        logStreamWriter?.Dispose();
        logFileStream?.Dispose();
    }

    public static void DisposeLogger()
    {
        Instance.Dispose();
    }

    private static string InitializeLogsFolder()
    {
        var workingDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var logsDirectory = System.IO.Path.Combine(workingDirectory, "logs");
        if (!System.IO.Directory.Exists(logsDirectory))
        {
            System.IO.Directory.CreateDirectory(logsDirectory);
        }

        return logsDirectory;
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public static Logger Instance { get; private set; } = null!;

    public static void InitializeLogger(string prefix = "UI", bool logToConsoleToo = false)
    {
        Instance ??= new Logger(prefix, logToConsoleToo);
    }

    public static void Log(LogLevel level, string message, Exception? ex = null, params object[] args)
    {
        var logMessage = $"[{FormatLogLevel(level)}] [{Instance._prefix}] {string.Format(message, args)}";
        if (ex != null)
        {
            logMessage += $"\nException: {ex}";
        }
        Instance.WriteLog(logMessage);
    }

    public static void LogDebug(string message, params object[] args)
    {
        if (string.IsNullOrEmpty(message.Trim()))
            return;

        Log(LogLevel.Debug, message, null, args);
    }

    public static void LogInfo(string message, params object[] args)
    {
        if (string.IsNullOrEmpty(message.Trim()))
            return;

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

    public void WriteLog(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"{timestamp} {message}";
        logStreamWriter.WriteLine(logMessage);

        if (_logToConsoleToo)
            Console.WriteLine(logMessage);
    }

    private static string FormatLogLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => "DBG",
            LogLevel.Info => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            _ => "N/A"
        };
    }

    public void Dispose()
    {
        DeinitializeLogStream();
        GC.SuppressFinalize(this);
    }
}
