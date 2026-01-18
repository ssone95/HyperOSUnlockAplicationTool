using HOSUnlock.Configuration;
using HOSUnlock.Constants;
using HOSUnlock.Enums;
using HOSUnlock.Models;
using HOSUnlock.Models.Base;
using HOSUnlock.Models.Common;
using HOSUnlock.Services;
using Terminal.Gui;

namespace HOSUnlock.Views;

public partial class MainView
{
    // UI Controls
    private Button _startButton = null!;
    private Button _stopButton = null!;
    private Label _dateInfoLabel = null!;

    // Timers
    private Timer? _dateInfoTimer;

    // Processing state - using interface
    private IMiAuthRequestProcessor? _miAuthRequestProcessor;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRunning;
    private bool _allThresholdsReached;
    private bool _userRequestedStop;
    private bool _disposed;

    // Track pending operations (same pattern as HeadlessApp)
    private int _pendingOperations;
    private readonly object _pendingOperationsLock = new();
    private TaskCompletionSource? _allOperationsCompleted;

    // Track operation results
    private readonly List<OperationResult> _operationResults = [];
    private readonly object _resultsLock = new();

    public MainView()
    {
        InitializeComponent();
    }

    public override async void OnLoaded()
    {
        base.OnLoaded();

        try
        {
            InitializeControls();
            await InitializeApplicationAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error occurred while loading the main view.", ex);
            ShowError($"An error occurred while loading:\n{ex.Message}");
        }
    }

    private void InitializeControls()
    {
        _startButton = new Button("_Start")
        {
            X = 0,
            Y = 0,
            Width = 10
        };

        _stopButton = new Button("S_top")
        {
            X = Pos.Right(_startButton) + 1,
            Y = 0,
            Width = 10,
            Enabled = false
        };

        _startButton.Clicked += OnStartClicked;
        _stopButton.Clicked += OnStopClicked;

        _dateInfoLabel = new Label(NtpConstants.DefaultTimeLabelText)
        {
            X = Pos.Right(_stopButton) + 2,
            Y = 0,
            Width = Dim.Fill(),
            TextAlignment = TextAlignment.Right
        };

        _optionsPanel.Add(_startButton, _stopButton, _dateInfoLabel);
    }

    private async Task InitializeApplicationAsync()
    {
        LogStatus("HOSUnlock - TUI Mode");
        LogStatus("Initializing application...");

        // Validate configuration
        if (AppConfiguration.Instance is null || !AppConfiguration.Instance.IsConfigurationValid())
        {
            LogStatus("[ERR] Configuration is invalid!");
            LogStatus("Please check appsettings.json file.");
            _startButton.Enabled = false;
            return;
        }

        LogStatus("[OK] Configuration loaded successfully.");

        // Display token shifts
        foreach (var (shift, index) in AppConfiguration.Instance.TokenShifts.Select((s, i) => (s, i + 1)))
        {
            LogStatus($"  Token Shift #{index}: {shift}ms");
        }

        // Auto-run check
        if (AppConfiguration.Instance.AutoRunOnStart)
        {
            LogStatus("Auto-run enabled. Starting automatically...");
            await Task.Delay(500).ConfigureAwait(false);
            Application.MainLoop?.Invoke(() => OnStartClicked());
        }
        else
        {
            LogStatus("Press 'Start' to begin monitoring.");
        }
    }

    private async void OnStartClicked()
    {
        if (_isRunning)
            return;

        // Check if ClockProvider needs to be reset (thresholds already reached from previous run)
        if (ClockProvider.Instance is not null &&
            await ClockProvider.Instance.WereAllThresholdsReachedAsync().ConfigureAwait(false))
        {
            if (!ClockProvider.Instance.CanRetry)
            {
                ShowError($"Maximum retry attempts ({ClockProvider.Instance.MaxRetryAttempts}) reached.\n\nPlease restart the application to continue.");
                return;
            }

            Application.MainLoop?.Invoke(() =>
            {
                LogApp($"Restarting... (Attempt {ClockProvider.Instance.AttemptCount + 1} of {ClockProvider.Instance.MaxRetryAttempts})");
            });

            var restarted = await ClockProvider.Instance.ResetAndRestartAsync().ConfigureAwait(false);
            if (!restarted)
            {
                ShowError("Failed to restart. Maximum retries reached.\n\nPlease restart the application.");
                return;
            }
        }

        _isRunning = true;
        _allThresholdsReached = false;
        _userRequestedStop = false;
        _allOperationsCompleted = new TaskCompletionSource();
        _cancellationTokenSource = new CancellationTokenSource();

        lock (_resultsLock)
        {
            _operationResults.Clear();
        }

        UpdateButtonStates(isRunning: true, isProcessingRequests: false);
        ClearAllPanels();

        try
        {
            await RunMonitoringAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            LogApp("Operation cancelled by user.");
        }
        catch (Exception ex)
        {
            LogApp($"[ERR] Fatal error: {ex.Message}");
            Logger.LogError("Fatal error in monitoring.", ex);
        }
        finally
        {
            await CleanupAsync().ConfigureAwait(false);
            _isRunning = false;
            UpdateButtonStates(isRunning: false, isProcessingRequests: false);

            // Handle completion based on auto-run mode
            await HandleCompletionAsync().ConfigureAwait(false);
        }
    }

    private async Task HandleCompletionAsync()
    {
        // If user explicitly requested stop, don't auto-retry
        if (_userRequestedStop)
        {
            LogApp("Operation stopped by user. Auto-retry disabled.");
            Application.MainLoop?.Invoke(() =>
            {
                _startButton.Text = "_Start";
                _startButton.Enabled = true;
            });
            return;
        }

        var isAutoRun = AppConfiguration.Instance?.AutoRunOnStart == true;
        var canRetry = ClockProvider.Instance?.CanRetry == true;
        var maxRetries = ClockProvider.Instance?.MaxRetryAttempts ?? AppConfiguration.DefaultMaxAutoRetries;

        if (canRetry)
        {
            if (isAutoRun)
            {
                // Auto-run mode: automatically restart
                LogApp($"Auto-run mode: Automatically retrying... ({ClockProvider.Instance!.RemainingAttempts} attempts left)");

                Application.MainLoop?.Invoke(() =>
                {
                    _startButton.Enabled = false;
                    _startButton.Text = "_Auto-retry...";
                });

                // Small delay before auto-retry
                await Task.Delay(2000).ConfigureAwait(false);

                Application.MainLoop?.Invoke(() => OnStartClicked());
            }
            else
            {
                // Manual mode: ask user if they want to retry
                Application.MainLoop?.Invoke(() =>
                {
                    var result = MessageBox.Query("Retry?",
                        $"Would you like to retry?\n\n" +
                        $"Remaining attempts: {ClockProvider.Instance!.RemainingAttempts}\n\n" +
                        "The next attempt will wait for tomorrow's threshold window.",
                        "Yes", "No");

                    if (result == 0) // Yes
                    {
                        _startButton.Text = $"_Restart ({ClockProvider.Instance.RemainingAttempts} left)";
                        _startButton.Enabled = true;
                        OnStartClicked();
                    }
                    else
                    {
                        // User declined - keep app open with restart option
                        _startButton.Text = $"_Restart ({ClockProvider.Instance.RemainingAttempts} left)";
                        _startButton.Enabled = true;
                        LogApp("Retry declined. You can manually restart using the 'Restart' button.");
                    }
                });
            }
        }
        else
        {
            // No more retries available
            Application.MainLoop?.Invoke(() =>
            {
                _startButton.Enabled = false;
                _startButton.Text = "_Start (Max retries)";

                if (!isAutoRun)
                {
                    MessageBox.Query("Max Retries Reached",
                        $"Maximum retry attempts ({maxRetries}) reached.\n\n" +
                        "Please restart the application if you need to try again.\n" +
                        "(Token may have expired after multiple days)",
                        "OK");
                }
                else
                {
                    LogApp($"[WRN] Maximum retry attempts reached ({maxRetries}). Application will remain open.");
                    LogApp("Please restart the application to try again.");
                }
            });
        }
    }

    private void OnStopClicked()
    {
        if (!_isRunning)
            return;

        LogApp("Stop requested by user...");
        _userRequestedStop = true;
        _cancellationTokenSource?.Cancel();
        _allOperationsCompleted?.TrySetResult();
    }

    private async Task RunMonitoringAsync()
    {
        // Initialize MiAuthRequestProcessor
        LogStatus("Initializing request processor...");
        _miAuthRequestProcessor = new MiAuthRequestProcessor(
            AppConfiguration.Instance!,
            _cancellationTokenSource!);

        // Run pre-checks
        LogApp("Running pre-checks...");
        var preCheckResults = await _miAuthRequestProcessor.StartAsync().ConfigureAwait(false);

        if (!VerifyAllPreCheckResults(preCheckResults))
        {
            LogApp("[ERR] Pre-check failed. Cannot proceed.");
            return;
        }

        LogApp("[OK] Pre-checks passed.");

        // Subscribe to clock events
        if (ClockProvider.Instance is null)
        {
            LogApp("[ERR] ClockProvider not initialized.");
            return;
        }

        ClockProvider.Instance.OnClockThresholdExceeded += OnClockThresholdExceeded;
        ClockProvider.Instance.OnAllThresholdsReached += OnAllThresholdsReached;

        // Display threshold times
        DisplayThresholdTimes();

        // Start time display timer
        StartTimeDisplayTimer();

        var attemptInfo = ClockProvider.Instance.AttemptCount > 0
            ? $" (Attempt {ClockProvider.Instance.AttemptCount + 1} of {ClockProvider.Instance.MaxRetryAttempts})"
            : "";
        LogApp($"Monitoring clock thresholds...{attemptInfo}");
        LogApp("Application will process when thresholds are reached.");

        // Monitor loop
        await MonitorThresholdsAsync().ConfigureAwait(false);

        // Wait for all pending operations
        LogApp("All thresholds triggered. Waiting for pending operations...");
        UpdateDateLabel(NtpConstants.TimeLabelText_WaitingForRequestsCompletion);

        if (_allOperationsCompleted is not null)
        {
            await _allOperationsCompleted.Task.ConfigureAwait(false);
        }

        LogApp("[OK] All operations completed!");
        LogResults("=======================================");
        LogResults("Processing complete!");
        LogResults("=======================================");

        UpdateDateLabel(NtpConstants.TimeLabelText_RequestsCompletedCloseApp);

        ShowCompletionMessage();
    }

    private void ShowCompletionMessage()
    {
        List<OperationResult> results;
        lock (_resultsLock)
        {
            results = [.. _operationResults];
        }

        var hasApproved = results.Any(r => r.Status == OperationStatus.Approved);
        var hasFailed = results.Any(r => r.Status == OperationStatus.Failed);
        var hasUnknown = results.Any(r => r.Status == OperationStatus.Unknown);

        string title;
        string message;

        if (hasApproved)
        {
            title = "Success!";
            message = "At least one unlock request was APPROVED!\n\n" +
                      "Try: Settings > Additional Settings > Developer Options > Mi Unlock Status > Add Account";
        }
        else if (hasFailed)
        {
            var failures = results.Where(r => r.Status == OperationStatus.Failed).ToList();
            var failureReasons = string.Join("\n", failures.Select(f => $"• {f.Identifier}: {f.Message}"));

            title = "Failed";
            message = $"Some operations failed:\n\n{failureReasons}";
        }
        else if (hasUnknown)
        {
            title = "Uncertain";
            message = "Operations completed but results are uncertain.\n\n" +
                      "Try: Settings > Additional Settings > Developer Options > Mi Unlock Status\n\n" +
                      "Check if your account shows unlock permission.";
        }
        else if (results.Count == 0)
        {
            title = "No Results";
            message = "No operations were processed.";
        }
        else
        {
            title = "Complete";
            message = "All operations completed.\n\n" +
                      "Try: Settings > Additional Settings > Developer Options > Mi Unlock Status > Add Account";
        }

        // Only show message box if not in auto-run mode (auto-run will show retry prompt)
        if (AppConfiguration.Instance?.AutoRunOnStart != true)
        {
            Application.MainLoop?.Invoke(() =>
            {
                if (hasFailed)
                    MessageBox.ErrorQuery(title, message, "OK");
                else
                    MessageBox.Query(title, message, "OK");
            });
        }
        else
        {
            // In auto-run mode, just log the result
            LogResults($"[{title}] {message.Replace("\n", " ")}");
        }
    }

    private void RecordResult(string identifier, OperationStatus status, string message)
    {
        lock (_resultsLock)
        {
            _operationResults.Add(new OperationResult(identifier, status, message));
        }
    }

    private bool VerifyAllPreCheckResults(
        Dictionary<TokenInfo, BaseResponse<BlCheckResponseDto>> preCheckResults)
    {
        foreach (var kvp in preCheckResults)
        {
            var identifier = $"Token #{kvp.Key.Index}";
            var result = EvaluateBlCheckResponse(kvp.Value, identifier);

            switch (result)
            {
                case MiEnums.MiAuthApplicationResult.ApplicationApproved:
                    LogStatus($"{identifier}: Already APPROVED!");
                    LogStatus("You can proceed to unlock your device.");
                    return false;

                case MiEnums.MiAuthApplicationResult.ApplicationRejected:
                    LogStatus($"{identifier}: Application REJECTED.");
                    return false;

                case MiEnums.MiAuthApplicationResult.Unknown:
                    LogStatus($"{identifier}: Status UNKNOWN.");
                    LogStatus("Please verify account status manually.");
                    return false;

                case MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved:
                    LogStatus($"{identifier}: Eligible for unlock request.");
                    continue;

                default:
                    LogStatus($"{identifier}: Unexpected status.");
                    return false;
            }
        }

        return true;
    }

    private async Task MonitorThresholdsAsync()
    {
        while (!_cancellationTokenSource!.Token.IsCancellationRequested &&
               !await ClockProvider.Instance!.WereAllThresholdsReachedAsync().ConfigureAwait(false))
        {
            await Task.Delay(1000, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
    }

    private void DisplayThresholdTimes()
    {
        var thresholds = ClockProvider.Instance!.GetThresholdSnapshots();

        LogThresholds("Scheduled threshold times:");
        LogThresholds("---------------------------------------");

        foreach (var kvp in thresholds.OrderBy(x => x.Key.TokenIndex).ThenBy(x => x.Key.ShiftIndex))
        {
            LogThresholds($"Token #{kvp.Key.TokenIndex} Shift #{kvp.Key.ShiftIndex}:");
            LogThresholds($"  Beijing: {kvp.Value.Beijing:yyyy-MM-dd HH:mm:ss.fff}");
            LogThresholds($"  UTC:     {kvp.Value.Utc:yyyy-MM-dd HH:mm:ss.fff}");
            LogThresholds($"  Local:   {kvp.Value.Local:yyyy-MM-dd HH:mm:ss.fff}");
            LogThresholds("");
        }
    }

    private void StartTimeDisplayTimer()
    {
        _dateInfoTimer = new Timer(
            _ => UpdateTimeDisplay(),
            state: null,
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromMilliseconds(NtpConstants.UITimerRefreshIntervalMilliseconds));
    }

    private void StopTimeDisplayTimer()
    {
        _dateInfoTimer?.Dispose();
        _dateInfoTimer = null;
    }

    private void UpdateTimeDisplay()
    {
        if (ClockProvider.Instance is null)
            return;

        var (localTime, utcTime, beijingTime) = ClockProvider.Instance.GetCurrentTimes();

        Application.MainLoop?.Invoke(() =>
        {
            _dateInfoLabel.Text =
                $"Beijing: {beijingTime:HH:mm:ss.fff} | " +
                $"UTC: {utcTime:HH:mm:ss.fff} | " +
                $"Local: {localTime:HH:mm:ss.fff}";
        });
    }

    #region Clock Event Handlers

    private void IncrementPendingOperations()
    {
        lock (_pendingOperationsLock)
        {
            _pendingOperations++;
        }
    }

    private void DecrementPendingOperations()
    {
        lock (_pendingOperationsLock)
        {
            _pendingOperations--;
            if (_pendingOperations == 0 && _allThresholdsReached)
            {
                _allOperationsCompleted?.TrySetResult();
            }
        }
    }

    private void CheckAllOperationsCompleted()
    {
        lock (_pendingOperationsLock)
        {
            if (_pendingOperations == 0)
            {
                _allOperationsCompleted?.TrySetResult();
            }
        }
    }

    private async void OnClockThresholdExceeded(object? sender, ClockProvider.ClockThresholdExceededEventArgs e)
    {
        IncrementPendingOperations();
        try
        {
            var details = e.TokenShiftDetails;
            var identifier = $"Token #{details.TokenIndex} Shift #{details.ShiftIndex}";

            LogApp($"[THRESHOLD] {identifier} exceeded!");
            LogApp($"  Beijing: {e.BeijingTime:yyyy-MM-dd HH:mm:ss.fff}");
            LogApp($"  UTC:     {e.UtcTime:yyyy-MM-dd HH:mm:ss.fff}");
            LogApp($"  Local:   {e.LocalTime:yyyy-MM-dd HH:mm:ss.fff}");

            UpdateButtonStates(isRunning: true, isProcessingRequests: true);

            var applicationResult = await _miAuthRequestProcessor!.ApplyForUnlockAsync(details).ConfigureAwait(false);
            await ProcessApplicationRequestResultAsync(applicationResult, details).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var identifier = $"Token #{e.TokenShiftDetails.TokenIndex} Shift #{e.TokenShiftDetails.ShiftIndex}";
            LogResults($"[ERR] Error processing threshold: {ex.Message}");
            Logger.LogError("Error in OnClockThresholdExceeded.", ex);
            RecordResult(identifier, OperationStatus.Failed, ex.Message);
        }
        finally
        {
            DecrementPendingOperations();
        }
    }

    private void OnAllThresholdsReached(object? sender, EventArgs e)
    {
        LogApp("All clock thresholds have been triggered!");
        _allThresholdsReached = true;
        StopTimeDisplayTimer();
        CheckAllOperationsCompleted();
    }

    #endregion

    #region Application Request Processing

    private async Task ProcessApplicationRequestResultAsync(
        BaseResponse<ApplyBlAuthResponseDto> applicationResult,
        TokenShiftDefinition shiftDetails)
    {
        var identifier = $"Token #{shiftDetails.TokenIndex} Shift #{shiftDetails.ShiftIndex}";

        if (applicationResult?.Data is null)
        {
            LogResults($"[ERR] {identifier}: Failed to retrieve result");
            LogResults($"  Code: {applicationResult?.Code}, Message: {applicationResult?.Message}");
            RecordResult(identifier, OperationStatus.Failed, $"No response data (Code: {applicationResult?.Code})");
            return;
        }

        LogResults($"--- {identifier} ---");

        switch (applicationResult.Code)
        {
            case MiDataConstants.STATUS_CODE_SUCCESS:
                LogResults($"[OK] Application submitted!");
                LogResults($"  Deadline: {applicationResult.Data.DeadlineFormat}");
                await RunDetailedApplicationStatusChecksAsync(applicationResult, shiftDetails).ConfigureAwait(false);
                break;

            case MiDataConstants.STATUS_REQUEST_POTENTIALLY_VALID:
                LogResults("[WRN] Application may already be submitted.");
                var result = await _miAuthRequestProcessor!.RunSingleBlCheckAsync(shiftDetails.ToTokenInfo()).ConfigureAwait(false);
                var checkResult = EvaluateBlCheckResponse(result, identifier);
                RecordResultFromCheckResponse(identifier, checkResult);
                break;

            case MiDataConstants.STATUS_COOKIE_EXPIRED:
                LogResults("[ERR] Cookie expired!");
                LogResults("Please update the cookie in configuration.");
                RecordResult(identifier, OperationStatus.Failed, "Cookie expired");
                break;

            case MiDataConstants.STATUS_REQUEST_REJECTED:
            case MiDataConstants.STATUS_CODE_OTHER_FAILURE:
                LogResults("[WRN] Application limit reached.");
                LogResults("You may only apply once every 30 days.");
                RecordResult(identifier, OperationStatus.Failed, "Application limit reached (30 days)");
                break;

            default:
                LogResults($"[ERR] Unknown response: {applicationResult.Code}");
                LogResults($"  Message: {applicationResult.Message}");
                RecordResult(identifier, OperationStatus.Failed, $"Unknown response: {applicationResult.Code}");
                break;
        }
    }

    private async Task RunDetailedApplicationStatusChecksAsync(
        BaseResponse<ApplyBlAuthResponseDto> response,
        TokenShiftDefinition shiftDefinition)
    {
        var identifier = $"Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex}";

        switch (response.Data!.ApplyResultState)
        {
            case MiEnums.MiApplyResult.ApplicationSuccessful:
                var blResult = await _miAuthRequestProcessor!.RunSingleBlCheckAsync(shiftDefinition.ToTokenInfo()).ConfigureAwait(false);
                var applicationResult = EvaluateBlCheckResponse(blResult, identifier);

                if (applicationResult == MiEnums.MiAuthApplicationResult.ApplicationApproved)
                {
                    LogResults("=======================================");
                    LogResults($"[OK] {identifier}: APPROVED!");
                    LogResults("Try to unlock the bootloader now!");
                    LogResults("=======================================");
                    RecordResult(identifier, OperationStatus.Approved, "Unlock approved!");
                }
                else if (applicationResult == MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved)
                {
                    LogResults($"[?] {identifier}: Uncertain state.");
                    LogResults("Try unlocking, or run again later.");
                    RecordResult(identifier, OperationStatus.Unknown, "Uncertain state - try unlocking");
                }
                else
                {
                    LogResults($"[X] {identifier}: Not approved.");
                    LogResults("Try again later.");
                    RecordResult(identifier, OperationStatus.Failed, "Not approved");
                }
                break;

            case MiEnums.MiApplyResult.LimitReached:
                LogResults($"[WRN] {identifier}: Limit reached.");
                LogResults($"  Try again on: {response.Data.DeadlineFormat ?? "Not specified"}");
                RecordResult(identifier, OperationStatus.Failed, $"Limit reached - try again on {response.Data.DeadlineFormat ?? "unknown date"}");
                break;

            case MiEnums.MiApplyResult.BlockedUntil:
                LogResults($"[ERR] {identifier}: Blocked.");
                LogResults($"  Try again on: {response.Data.DeadlineFormat ?? "Not specified"}");
                RecordResult(identifier, OperationStatus.Failed, $"Blocked until {response.Data.DeadlineFormat ?? "unknown date"}");
                break;

            default:
                LogResults($"[?] {identifier}: Unknown result.");
                RecordResult(identifier, OperationStatus.Unknown, "Unknown result");
                break;
        }
    }

    private void RecordResultFromCheckResponse(string identifier, MiEnums.MiAuthApplicationResult result)
    {
        switch (result)
        {
            case MiEnums.MiAuthApplicationResult.ApplicationApproved:
                RecordResult(identifier, OperationStatus.Approved, "Already approved");
                break;
            case MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved:
                RecordResult(identifier, OperationStatus.Unknown, "May be approved - check manually");
                break;
            case MiEnums.MiAuthApplicationResult.ApplicationRejected:
                RecordResult(identifier, OperationStatus.Failed, "Application rejected");
                break;
            default:
                RecordResult(identifier, OperationStatus.Unknown, "Unknown status");
                break;
        }
    }

    private MiEnums.MiAuthApplicationResult EvaluateBlCheckResponse(
        BaseResponse<BlCheckResponseDto> response,
        string identifier)
    {
        if (response.Code == MiDataConstants.STATUS_COOKIE_EXPIRED)
        {
            LogResults($"[ERR] {identifier}: Cookie expired!");
            throw new InvalidOperationException($"{identifier}: Cookie expired.");
        }

        LogApp($"{identifier}: is_pass={response.Data!.IsPass}, button_state={response.Data.ButtonState}");

        if (response.Data.MiEnumIsPass == MiEnums.MiIsPassState.Unknown)
        {
            LogResults($"[?] {identifier}: is_pass UNKNOWN");
            return MiEnums.MiAuthApplicationResult.Unknown;
        }

        if (response.Data.MiEnumIsPass == MiEnums.MiIsPassState.RequestApproved)
        {
            LogResults($"[OK] {identifier}: Already approved!");
            return MiEnums.MiAuthApplicationResult.ApplicationApproved;
        }

        return response.Data.MiEnumButtonState switch
        {
            MiEnums.MiButtonState.RequestSubmissionPossible =>
                MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved,

            MiEnums.MiButtonState.AccountBlockedFromApplyingUntilDate =>
                MiEnums.MiAuthApplicationResult.ApplicationRejected,

            MiEnums.MiButtonState.AccountCreatedLessThan30DaysAgo =>
                MiEnums.MiAuthApplicationResult.ApplicationRejected,

            _ => MiEnums.MiAuthApplicationResult.Unknown
        };
    }

    #endregion

    #region Cleanup

    private async Task CleanupAsync()
    {
        StopTimeDisplayTimer();

        if (ClockProvider.Instance is not null)
        {
            ClockProvider.Instance.OnClockThresholdExceeded -= OnClockThresholdExceeded;
            ClockProvider.Instance.OnAllThresholdsReached -= OnAllThresholdsReached;
        }

        var processor = Interlocked.Exchange(ref _miAuthRequestProcessor, null);
        if (processor is not null)
        {
            await processor.StopAsync().ConfigureAwait(false);
            processor.Dispose();
        }

        var cts = Interlocked.Exchange(ref _cancellationTokenSource, null);
        if (cts is not null)
        {
            try { cts.Cancel(); } catch (ObjectDisposedException) { }
            cts.Dispose();
        }

        LogApp("Cleanup completed.");
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        _disposed = true;

        if (disposing)
        {
            StopTimeDisplayTimer();

            var cts = Interlocked.Exchange(ref _cancellationTokenSource, null);
            if (cts is not null)
            {
                try { cts.Cancel(); } catch (ObjectDisposedException) { }
                cts.Dispose();
            }

            var processor = Interlocked.Exchange(ref _miAuthRequestProcessor, null);
            processor?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion

    #region UI Helpers

    private void UpdateButtonStates(bool isRunning, bool isProcessingRequests)
    {
        Application.MainLoop?.Invoke(() =>
        {
            _startButton.Enabled = !isProcessingRequests && !isRunning;
            _stopButton.Enabled = !isProcessingRequests && isRunning;
        });
    }

    private void UpdateDateLabel(string text)
    {
        Application.MainLoop?.Invoke(() =>
        {
            _dateInfoLabel.Text = text;
        });
    }

    private void ClearAllPanels()
    {
        _statusPanel.Clear();
        _thresholdsPanel.Clear();
        _applicationLogsPanel.Clear();
        _requestResultsPanel.Clear();
    }

    // Convenience logging methods for each panel
    private void LogStatus(String message) => _statusPanel.Log(message);
    private void LogApp(String message) => _applicationLogsPanel.Log(message);
    private void LogResults(String message) => _requestResultsPanel.LogRaw(message);
    private void LogThresholds(String message) => _thresholdsPanel.LogRaw(message);

    private void ShowError(String message)
    {
        Application.MainLoop?.Invoke(() =>
        {
            MessageBox.ErrorQuery("Error", message, "OK");
        });
    }

    #endregion

    #region Result Tracking

    private enum OperationStatus
    {
        Approved,
        Failed,
        Unknown
    }

    private sealed record OperationResult(string Identifier, OperationStatus Status, string Message);

    #endregion
}
