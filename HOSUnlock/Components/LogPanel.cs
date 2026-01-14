using HOSUnlock.Services;
using Terminal.Gui;

namespace HOSUnlock.Components;

/// <summary>
/// A scrollable log panel that displays messages with timestamps.
/// Thread-safe for use with async operations.
/// Also writes all logs to the file-based Logger.
/// </summary>
public sealed class LogPanel : FrameView
{
    private readonly ListView _listView;
    private readonly List<string> _entries = [];
    private readonly object _lock = new();
    private readonly int _maxEntries;
    private readonly string _panelName;
    private bool _autoScroll = true;

    public LogPanel(string title, int maxEntries = 500)
    {
        _maxEntries = maxEntries;
        _panelName = title;

        Title = title;
        Border.BorderStyle = BorderStyle.Single;
        Border.Effect3D = false;

        _listView = new ListView(new List<string>())
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            AllowsMarking = false,
            AllowsMultipleSelection = false
        };

        Add(_listView);
    }

    /// <summary>
    /// Gets or sets whether the log automatically scrolls to the bottom when new entries are added.
    /// </summary>
    public bool AutoScroll
    {
        get => _autoScroll;
        set => _autoScroll = value;
    }

    /// <summary>
    /// Appends a log message with timestamp. Thread-safe.
    /// Also writes to the file-based Logger.
    /// </summary>
    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var entry = $"[{timestamp}] {message}";

        Logger.LogInfo($"[{_panelName}] {message}");

        AddEntry(entry);
    }

    /// <summary>
    /// Appends a log message without timestamp. Thread-safe.
    /// Also writes to the file-based Logger.
    /// </summary>
    public void LogRaw(string message)
    {
        Logger.LogInfo($"[{_panelName}] {message}");

        AddEntry(string.IsNullOrEmpty(message) ? " " : message);
    }

    /// <summary>
    /// Appends an info-level log message.
    /// </summary>
    public void LogInfo(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var entry = $"[{timestamp}] [INF] {message}";

        Logger.LogInfo($"[{_panelName}] {message}");

        AddEntry(entry);
    }

    /// <summary>
    /// Appends a warning-level log message.
    /// </summary>
    public void LogWarning(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var entry = $"[{timestamp}] [WRN] {message}";

        Logger.LogWarning($"[{_panelName}] {message}");

        AddEntry(entry);
    }

    /// <summary>
    /// Appends an error-level log message.
    /// </summary>
    public void LogError(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var entry = $"[{timestamp}] [ERR] {message}";

        Logger.LogError($"[{_panelName}] {message}");

        AddEntry(entry);
    }

    /// <summary>
    /// Clears all log entries. Thread-safe.
    /// </summary>
    public void Clear()
    {
        if (Application.MainLoop is null)
        {
            ClearInternal();
            return;
        }

        Application.MainLoop.Invoke(ClearInternal);
    }

    private void ClearInternal()
    {
        lock (_lock)
        {
            _entries.Clear();
            _listView.SetSource(new List<string>());
        }
    }

    private void AddEntry(string entry)
    {
        if (Application.MainLoop is null)
        {
            AddEntryInternal(entry);
            return;
        }

        Application.MainLoop.Invoke(() => AddEntryInternal(entry));
    }

    private void AddEntryInternal(string entry)
    {
        List<string> snapshot;
        int count;

        lock (_lock)
        {
            _entries.Add(entry);

            while (_entries.Count > _maxEntries)
            {
                _entries.RemoveAt(0);
            }

            snapshot = [.. _entries];
            count = snapshot.Count;
        }

        _listView.SetSource(snapshot);

        if (_autoScroll && count > 0)
        {
            _listView.SelectedItem = count - 1;
            _listView.TopItem = Math.Max(0, count - _listView.Bounds.Height);
        }
    }
}
