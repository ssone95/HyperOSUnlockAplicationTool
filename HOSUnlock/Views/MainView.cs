using HOSUnlock.Components;
using HOSUnlock.Constants;
using HOSUnlock.Services;
using Terminal.Gui;

namespace HOSUnlock.Views;

public partial class MainView
{
    /// <summary>
    /// Public accessors for the panels if you need to modify them from outside
    /// </summary>
    public TitledPanel TopLeftPanel => topLeftPanel;
    public TitledPanel TopRightPanel => topRightPanel;
    public TitledPanel BottomLeftPanel => bottomLeftPanel;
    public TitledPanel BottomRightPanel => bottomRightPanel;
    public TitledPanel BottomOptionsPanel => bottomOptionsPanel;

    private Button _startButton = null!;
    private Button _stopButton = null!;
    private Label _dateInfoLabel = null!;
    private Timer? _dateInfoTimer;

    public MainView()
    {
        InitializeComponent();
    }

    public override async void OnLoaded()
    {
        base.OnLoaded();

        try
        {
            await PopulatePanelsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error occurred while loading the main view: {0}", ex, ex.Message);
            MessageBox.ErrorQuery("Error", $"An error occurred while loading the main view:\n{ex.Message}", "OK");
            Application.Shutdown();
        }
    }

    private Task PopulatePanelsAsync()
    {
        var label1 = new Label("Content for Panel 1")
        {
            X = Pos.Center(),
            Y = Pos.Center()
        };
        topLeftPanel.AddContent(label1);

        var label2 = new Label("Content for Panel 2")
        {
            X = Pos.Center(),
            Y = Pos.Center()
        };
        topRightPanel.AddContent(label2);

        var label3 = new Label("Content for Panel 3")
        {
            X = Pos.Center(),
            Y = Pos.Center()
        };
        bottomLeftPanel.AddContent(label3);

        var label4 = new Label("Content for Panel 4")
        {
            X = Pos.Center(),
            Y = Pos.Center()
        };
        bottomRightPanel.AddContent(label4);

        _startButton = new Button("_Start")
        {
            X = 0,
            Y = 0,
            Width = 8
        };

        _stopButton = new Button("S_top")
        {
            X = Pos.Right(_startButton) + 1,
            Y = 0,
            Width = 8,
            Enabled = false
        };

        _startButton.Clicked += async () =>
        {
            Logger.LogInfo("Start button clicked");
            MessageBox.Query("Info", "Start button clicked!", "OK");
            UpdateButtonStates(isRunning: true, isProcessingRequests: false);
            await StartTimersAsync().ConfigureAwait(false);
        };

        _stopButton.Clicked += () =>
        {
            Logger.LogInfo("Stop button clicked");
            MessageBox.Query("Info", "Stop button clicked!", "OK");
            UpdateButtonStates(isRunning: false, isProcessingRequests: false);
            StopTimers();
        };

        _dateInfoLabel = new Label(NtpConstants.DefaultTimeLabelText)
        {
            X = Pos.Right(_stopButton) + 2,
            Y = 0,
            Width = Dim.Fill(),
            TextAlignment = Terminal.Gui.TextAlignment.Right
        };

        bottomOptionsPanel.AddContent(_startButton);
        bottomOptionsPanel.AddContent(_stopButton);
        bottomOptionsPanel.AddContent(_dateInfoLabel);

        return Task.CompletedTask;
    }

    private void UpdateButtonStates(bool isRunning, bool isProcessingRequests)
    {
        Application.MainLoop.Invoke(() =>
        {
            _startButton.Enabled = !isProcessingRequests && !isRunning;
            _stopButton.Enabled = !isProcessingRequests && isRunning;
        });
    }

    private void StopTimers()
    {
        Application.MainLoop.Invoke(() =>
        {
            _dateInfoLabel.Text = NtpConstants.DefaultTimeLabelText;
        });

        if (_dateInfoTimer is not null)
        {
            if (ClockProvider.Instance is not null)
            {
                ClockProvider.Instance.OnClockThresholdExceeded -= TriggerClockThresholdExceeded;
                ClockProvider.Instance.OnAllThresholdsReached -= TriggerAllThresholdsReached;
            }

            _dateInfoTimer.Dispose();
            _dateInfoTimer = null;
        }
    }

    private Task StartTimersAsync()
    {
        if (ClockProvider.Instance is null)
            return Task.CompletedTask;

        ClockProvider.Instance.OnClockThresholdExceeded += TriggerClockThresholdExceeded;
        ClockProvider.Instance.OnAllThresholdsReached += TriggerAllThresholdsReached;

        _dateInfoTimer = new Timer(
            _ => UpdateDateInfo(),
            state: null,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromMilliseconds(NtpConstants.UITimerRefreshIntervalMilliseconds));

        return Task.CompletedTask;
    }

    private async void TriggerAllThresholdsReached(object? sender, EventArgs e)
    {
        try
        {
            Logger.LogDebug("All clock thresholds have been reached, waiting for requests to be done.");
            StopTimers();
            UpdateButtonStates(isRunning: false, isProcessingRequests: true);

            Application.MainLoop.Invoke(() =>
            {
                _dateInfoLabel.Text = NtpConstants.TimeLabelText_WaitingForRequestsCompletion;
            });

            await Task.Delay(3000).ConfigureAwait(false);

            Application.MainLoop.Invoke(() =>
            {
                Logger.LogInfo("All clock thresholds have been reached! Try using: Settings -> Additional Settings -> Developer Options -> Mi Unlock Status -> Add Account");
                MessageBox.Query("Clock Alert",
                    "All clock thresholds have been reached!\n" +
                    "Try and use the \"Settings -> Additional Settings -> Developer Options -> Mi Unlock Status -> Add Account\" option.",
                    "OK");
                _dateInfoLabel.Text = NtpConstants.TimeLabelText_RequestsCompletedCloseApp;
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Error in TriggerAllThresholdsReached handler.", ex);
        }
    }

    private void TriggerClockThresholdExceeded(object? sender, ClockProvider.ClockThresholdExceededArgs e)
    {
        try
        {
            var shiftDetails = e.TokenShiftDetails;
            Logger.LogDebug($"Clock threshold exceeded: Token #{shiftDetails.TokenIndex} Shift #{shiftDetails.ShiftIndex}, Beijing Time: {e.BeijingTime}, UTC Time: {e.UtcTime}, Local Time: {e.LocalTime}");
            UpdateButtonStates(isRunning: true, isProcessingRequests: true);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error in TriggerClockThresholdExceeded handler.", ex);
        }
    }

    private void UpdateDateInfo()
    {
        if (ClockProvider.Instance is null)
            return;

        var (localTime, utcTime, beijingTime) = ClockProvider.Instance.GetCurrentTimes();

        Application.MainLoop.Invoke(() =>
        {
            _dateInfoLabel.Text =
                $"Beijing: {beijingTime:HH:mm:ss.fff MM/dd/yyyy} | " +
                $"UTC: {utcTime:HH:mm:ss.fff MM/dd/yyyy} | " +
                $"Local: {localTime:HH:mm:ss.fff MM/dd/yyyy}";
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopTimers();
        }
        base.Dispose(disposing);
    }
}
