using HOSUnlock.Configuration;
using HOSUnlock.Constants;
using HOSUnlock.Enums;
using HOSUnlock.Models;
using HOSUnlock.Models.Base;
using HOSUnlock.Models.Common;
using HOSUnlock.Services;

namespace HOSUnlock;

public static class HeadlessApp
{
    private static CancellationTokenSource? _cancellationTokenSource;
    private static bool _allThresholdsReached;
    private static MiAuthRequestProcessor? _miAuthRequestProcessor;
    private static readonly SemaphoreSlim _applicationProcessingSemaphore = new(1, 1);

    // Track pending threshold operations
    private static int _pendingOperations;
    private static readonly object _pendingOperationsLock = new();
    private static TaskCompletionSource _allOperationsCompleted = new();

    // Track if we should auto-retry
    private static bool _autoRetryEnabled;

    public static async Task Run(string[] args)
    {
        Logger.InitializeLogger("Headless", logToConsoleToo: true);
        _cancellationTokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Logger.LogInfo("Received interrupt signal. Shutting down...");
            _cancellationTokenSource?.Cancel();
        };

        try
        {
            Logger.LogInfo("HOSUnlock - Headless Mode");
            Logger.LogInfo("Press Ctrl+C to stop");

            await AppConfiguration.LoadAsync().ConfigureAwait(false);

            if (AppConfiguration.Instance is null || !AppConfiguration.Instance.IsConfigurationValid())
            {
                Logger.LogError("Application configuration is invalid.");
                Logger.LogError("Please check whether the appsettings.json file is valid and present in the same directory as the program.");
                return;
            }

            // Handle --auto-run argument
            if (args.Any(y => string.Equals(y, "--auto-run", StringComparison.OrdinalIgnoreCase)))
            {
                AppConfiguration.Instance.AutoRunOnStart = true;
            }

            _autoRetryEnabled = AppConfiguration.Instance.AutoRunOnStart;

            Logger.LogInfo("Configuration loaded successfully.");
            Logger.LogInfo($"Auto-run mode: {(_autoRetryEnabled ? "ENABLED" : "DISABLED")}");

            foreach (var (shift, index) in AppConfiguration.Instance.TokenShifts.Select((s, i) => (s, i + 1)))
            {
                Logger.LogInfo($"Token Shift #{index}: {shift}ms");
            }

            if (!AppConfiguration.Instance.AutoRunOnStart)
            {
                Logger.LogInfo("Auto-run on start is disabled. Press Enter to begin monitoring \nor any other key to exit:");
                var key = Console.ReadKey();
                Console.WriteLine(); // New line after key press
                if (key.Key != ConsoleKey.Enter)
                {
                    Logger.LogInfo("Non-Enter key pressed. Exiting application.");
                    return;
                }
            }
            else
            {
                Logger.LogInfo("Auto-run on start is enabled. Beginning monitoring...");
            }

            await ClockProvider.InitializeAsync().ConfigureAwait(false);
            Logger.LogInfo("Clock provider initialized.");

            // Main monitoring loop with retry support
            await RunWithRetryAsync().ConfigureAwait(false);

            // Keep app open if not in auto-run mode
            if (!_autoRetryEnabled)
            {
                Logger.LogInfo("========================================");
                Logger.LogInfo("Application process completed.");
                Logger.LogInfo("Please check the status details above and proceed according to instructions.");
                Logger.LogInfo("Press any key to terminate the application.");
                Logger.LogInfo("========================================");
                Console.ReadKey();
            }
            else
            {
                // In auto-run mode with max retries reached, also keep open but with different message
                if (ClockProvider.Instance is not null && !ClockProvider.Instance.CanRetry)
                {
                    Logger.LogWarning("========================================");
                    Logger.LogWarning("Maximum retry attempts reached (5).");
                    Logger.LogWarning("Token may have expired after 5 days of trying.");
                    Logger.LogWarning("Please restart the application to try again.");
                    Logger.LogWarning("Press any key to terminate the application.");
                    Logger.LogWarning("========================================");
                    Console.ReadKey();
                }
            }

            Logger.LogInfo("Shutting down application...");
        }
        catch (OperationCanceledException)
        {
            Logger.LogInfo("Operation cancelled by user.");
        }
        catch (Exception ex)
        {
            Logger.LogError("Fatal error occurred: {0}", ex, ex.Message);
        }
        finally
        {
            await CleanupAsync().ConfigureAwait(false);
        }
    }

    private static async Task RunWithRetryAsync()
    {
        var isFirstRun = true;

        while (!_cancellationTokenSource!.Token.IsCancellationRequested)
        {
            if (!isFirstRun)
            {
                // This is a retry - check if we can retry
                if (ClockProvider.Instance is null || !ClockProvider.Instance.CanRetry)
                {
                    Logger.LogWarning("Maximum retry attempts reached (5). Cannot retry.");
                    break;
                }

                // Ask for confirmation if not in auto-run mode
                if (!_autoRetryEnabled)
                {
                    Logger.LogInfo("========================================");
                    Logger.LogInfo($"Would you like to retry? (Remaining attempts: {ClockProvider.Instance.RemainingAttempts})");
                    Logger.LogInfo("The next attempt will wait for tomorrow's threshold window.");
                    Logger.LogInfo("Press Enter to retry, or any other key to exit:");
                    Logger.LogInfo("========================================");
                    
                    var key = Console.ReadKey();
                    Console.WriteLine(); // New line after key press
                    
                    if (key.Key != ConsoleKey.Enter)
                    {
                        Logger.LogInfo("Retry declined by user.");
                        break;
                    }
                    
                    Logger.LogInfo("Retry confirmed by user.");
                }
                else
                {
                    Logger.LogInfo("Auto-run mode: Automatically retrying...");
                }

                Logger.LogInfo("Preparing for retry attempt {0} of 5...", ClockProvider.Instance.AttemptCount + 1);

                var restarted = await ClockProvider.Instance.ResetAndRestartAsync().ConfigureAwait(false);
                if (!restarted)
                {
                    Logger.LogWarning("Failed to restart. Maximum retries reached.");
                    break;
                }

                // Reset operation tracking for new attempt
                _allOperationsCompleted = new TaskCompletionSource();
                _allThresholdsReached = false;
                _pendingOperations = 0;
            }

            isFirstRun = false;

            // Initialize request processor for this attempt
            _miAuthRequestProcessor?.Dispose();
            _miAuthRequestProcessor = new MiAuthRequestProcessor(AppConfiguration.Instance!, _cancellationTokenSource);
            Logger.LogInfo("MiAuth request processor started.");

            var preCheckResults = await _miAuthRequestProcessor.StartAsync().ConfigureAwait(false);

            if (!await VerifyAllPreCheckResults(preCheckResults).ConfigureAwait(false))
            {
                // Pre-check failed - don't retry for approval/rejection states
                break;
            }

            if (!_autoRetryEnabled)
            {
                Logger.LogInfo("Press Enter to continue monitoring the clock thresholds \nor any other key to exit:");
                var key = Console.ReadKey();
                Console.WriteLine(); // New line after key press
                if (key.Key != ConsoleKey.Enter)
                {
                    Logger.LogInfo("Non-Enter key pressed. Exiting monitoring.");
                    break;
                }
            }

            // Subscribe to clock events
            ClockProvider.Instance!.OnClockThresholdExceeded += OnClockThresholdExceeded;
            ClockProvider.Instance.OnAllThresholdsReached += OnAllThresholdsReached;

            DisplayThresholdTimes();

            Logger.LogInfo("Monitoring clock thresholds... (Attempt {0} of 5)",
                ClockProvider.Instance.AttemptCount + 1);
            
            if (_autoRetryEnabled)
            {
                Logger.LogInfo("Auto-run mode: Application will automatically retry if needed (up to 5 attempts).");
            }
            else
            {
                Logger.LogInfo("You will be prompted before any retry attempt.");
            }

            await MonitorThresholdsAsync().ConfigureAwait(false);

            // Wait for all pending unlock operations to complete
            Logger.LogInfo("All thresholds triggered. Waiting for pending operations to complete...");
            await _allOperationsCompleted.Task.ConfigureAwait(false);

            Logger.LogInfo("All operations completed for this attempt!");

            // Unsubscribe from events before potential retry
            ClockProvider.Instance.OnClockThresholdExceeded -= OnClockThresholdExceeded;
            ClockProvider.Instance.OnAllThresholdsReached -= OnAllThresholdsReached;

            // Check if we can/should retry
            if (!ClockProvider.Instance.CanRetry)
            {
                Logger.LogWarning("No more retry attempts available.");
                break;
            }

            // In auto-run mode, continue the loop for auto-retry
            // In manual mode, the loop will continue and ask for confirmation at the top
            if (_autoRetryEnabled)
            {
                Logger.LogInfo("Will automatically retry. Remaining attempts: {0}",
                    ClockProvider.Instance.RemainingAttempts);
            }
            // Loop continues - either auto-retry or ask user
        }
    }

    private static async Task<bool> VerifyAllPreCheckResults(
        Dictionary<TokenInfo, BaseResponse<BlCheckResponseDto>> preCheckResults)
    {
        foreach (var kvp in preCheckResults)
        {
            var result = EvaluateBlCheckResponse(kvp.Value, $"Token #{kvp.Key.Index}");

            switch (result)
            {
                case MiEnums.MiAuthApplicationResult.ApplicationApproved:
                case MiEnums.MiAuthApplicationResult.ApplicationRejected:
                    Logger.LogInfo("Since the application is already approved or rejected, its clock threshold will not be monitored.");
                    Logger.LogInfo("Application will exit now.");
                    return false;

                case MiEnums.MiAuthApplicationResult.Unknown:
                    Logger.LogWarning("Pre-run verification returned UNKNOWN status. Please verify the account status manually.");
                    Logger.LogWarning("Application will exit now.");
                    return false;

                case MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved:
                    continue;

                default:
                    Logger.LogWarning("Application cannot proceed due to pre-run verification failures.");
                    return false;
            }
        }

        return true;
    }

    private static async Task MonitorThresholdsAsync()
    {
        while (!_cancellationTokenSource!.Token.IsCancellationRequested &&
               !await ClockProvider.Instance!.WereAllThresholdsReachedAsync().ConfigureAwait(false))
        {
            await Task.Delay(1000, _cancellationTokenSource.Token).ConfigureAwait(false);

            if (DateTime.Now.Second % 30 == 0)
            {
                var (localTime, utcTime, beijingTime) = ClockProvider.Instance.GetCurrentTimes();
                Logger.LogInfo(
                    $"Beijing: {beijingTime:yyyy-MM-dd HH:mm:ss.fff} | UTC: {utcTime:yyyy-MM-dd HH:mm:ss.fff} | Local: {localTime:yyyy-MM-dd HH:mm:ss.fff}\n" +
                    "You can always interrupt the process using Ctrl+C key combination.");
            }
        }
    }

    private static async Task CleanupAsync()
    {
        if (_miAuthRequestProcessor is not null)
        {
            await _miAuthRequestProcessor.StopAsync().ConfigureAwait(false);
            _miAuthRequestProcessor.Dispose();
        }

        if (ClockProvider.Instance is not null)
        {
            ClockProvider.Instance.OnClockThresholdExceeded -= OnClockThresholdExceeded;
            ClockProvider.Instance.OnAllThresholdsReached -= OnAllThresholdsReached;
            ClockProvider.DisposeInstance();
        }

        Logger.LogInfo("Application shutdown complete.");
        _cancellationTokenSource?.Dispose();
        Logger.DisposeLogger();
    }

    private static void DisplayThresholdTimes()
    {
        var thresholds = ClockProvider.Instance!.GetThresholdSnapshots();
        Logger.LogInfo("Threshold times:");

        foreach (var kvp in thresholds.OrderBy(x => x.Key.TokenIndex).ThenBy(x => x.Key.ShiftIndex))
        {
            Logger.LogInfo(
                $"  TokenThreshold Token #{kvp.Key.TokenIndex} Shift #{kvp.Key.ShiftIndex}: " +
                $"Beijing: {kvp.Value.Beijing:yyyy-MM-dd HH:mm:ss.fff} | " +
                $"UTC: {kvp.Value.Utc:yyyy-MM-dd HH:mm:ss.fff} | " +
                $"Local: {kvp.Value.Local:yyyy-MM-dd HH:mm:ss.fff}");
        }
    }

    private static void IncrementPendingOperations()
    {
        lock (_pendingOperationsLock)
        {
            _pendingOperations++;
        }
    }

    private static void DecrementPendingOperations()
    {
        lock (_pendingOperationsLock)
        {
            _pendingOperations--;
            if (_pendingOperations == 0 && _allThresholdsReached)
            {
                _allOperationsCompleted.TrySetResult();
            }
        }
    }

    private static void CheckAllOperationsCompleted()
    {
        lock (_pendingOperationsLock)
        {
            if (_pendingOperations == 0)
            {
                _allOperationsCompleted.TrySetResult();
            }
        }
    }

    private static async void OnClockThresholdExceeded(object? sender, ClockProvider.ClockThresholdExceededArgs e)
    {
        IncrementPendingOperations();
        try
        {
            var details = e.TokenShiftDetails;

            Logger.LogInfo($"[THRESHOLD] Token #{details.TokenIndex} Shift #{details.ShiftIndex} threshold exceeded!");
            Logger.LogInfo($"            Beijing Time: {e.BeijingTime:yyyy-MM-dd HH:mm:ss.fff}");
            Logger.LogInfo($"            UTC Time: {e.UtcTime:yyyy-MM-dd HH:mm:ss.fff}");
            Logger.LogInfo($"            Local Time: {e.LocalTime:yyyy-MM-dd HH:mm:ss.fff}");

            var applicationResult = await _miAuthRequestProcessor!.ApplyForUnlockAsync(details).ConfigureAwait(false);
            await ProcessApplicationRequestResultAsync(applicationResult, details).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error in OnClockThresholdExceeded: {0}", ex, ex.Message);
        }
        finally
        {
            DecrementPendingOperations();
        }
    }

    private static async Task ProcessApplicationRequestResultAsync(
        BaseResponse<ApplyBlAuthResponseDto> applicationResult,
        TokenShiftDefinition shiftDetails)
    {
        var identifier = $"Token #{shiftDetails.TokenIndex} Shift #{shiftDetails.ShiftIndex}";

        if (applicationResult?.Data is null)
        {
            Logger.LogError($"MiDataService {identifier} - Failed to retrieve application result. " +
                           $"Response Code: {applicationResult?.Code}, Message: {applicationResult?.Message}");
            return;
        }

        await _applicationProcessingSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            switch (applicationResult.Code)
            {
                case MiDataConstants.STATUS_CODE_SUCCESS:
                    Logger.LogInfo($"MiDataService {identifier} - Application details available! Deadline received: {applicationResult.Data.DeadlineFormat}");
                    Logger.LogInfo($"MiDataService {identifier} - Checking the detailed application status...");
                    await RunDetailedApplicationStatusChecksAsync(applicationResult, shiftDetails).ConfigureAwait(false);
                    break;

                case MiDataConstants.STATUS_REQUEST_POTENTIALLY_VALID:
                    Logger.LogWarning($"MiDataService {identifier} - Application may have already been submitted. Deadline received: {applicationResult.Data.DeadlineFormat}");
                    Logger.LogInfo($"MiDataService {identifier} - Checking the detailed application status...");
                    var result = await _miAuthRequestProcessor!.RunSingleBlCheckAsync(shiftDetails.ToTokenInfo()).ConfigureAwait(false);
                    EvaluateBlCheckResponse(result, identifier);
                    break;

                case MiDataConstants.STATUS_COOKIE_EXPIRED:
                    Logger.LogError($"MiDataService {identifier} - Cookie expired. Please update the cookie in the configuration.");
                    break;

                case MiDataConstants.STATUS_REQUEST_REJECTED:
                case MiDataConstants.STATUS_CODE_OTHER_FAILURE:
                    Logger.LogWarning($"MiDataService {identifier} - Application limit reached. You may only apply once every 30 days.");
                    break;

                default:
                    Logger.LogError($"MiDataService {identifier} - Unknown response code: {applicationResult.Code}. Message: {applicationResult.Message}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Error in ProcessApplicationRequestResult: {0}", ex, ex.Message);
        }
        finally
        {
            _applicationProcessingSemaphore.Release();
        }
    }

    private static async Task RunDetailedApplicationStatusChecksAsync(
        BaseResponse<ApplyBlAuthResponseDto> response,
        TokenShiftDefinition shiftDefinition)
    {
        var identifier = $"Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex}";

        switch (response.Data!.ApplyResultState)
        {
            case MiEnums.MiApplyResult.ApplicationSuccessful:
                var blResult = await _miAuthRequestProcessor!.RunSingleBlCheckAsync(shiftDefinition.ToTokenInfo()).ConfigureAwait(false);
                var applicationResult = EvaluateBlCheckResponse(blResult, identifier);

                var message = applicationResult switch
                {
                    MiEnums.MiAuthApplicationResult.ApplicationApproved =>
                        $"MiDataService {identifier}: Unlock request was approved, please try to unlock the boot loader on your device!",
                    MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved =>
                        $"MiDataService {identifier}: Unlock request is in an uncertain state, try to unlock the boot loader, if that doesn't work - please try and run the application again for a new approval request.",
                    _ =>
                        $"MiDataService {identifier}: Unlock request was not approved, please try again at a later time depending on the state of your account."
                };

                if (applicationResult == MiEnums.MiAuthApplicationResult.ApplicationApproved ||
                    applicationResult == MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved)
                    Logger.LogInfo(message);
                else
                    Logger.LogError(message);
                break;

            case MiEnums.MiApplyResult.LimitReached:
                Logger.LogWarning($"MiDataService {identifier}: Request limit reached, try again on {response.Data.DeadlineFormat ?? "Not Specified"} date (format is Month/Day of the current year).");
                break;

            case MiEnums.MiApplyResult.BlockedUntil:
                Logger.LogError($"MiDataService {identifier}: Request was blocked, try again on {response.Data.DeadlineFormat ?? "NotSpecified"} date (format is Month/Day of the current year).");
                break;

            default:
                Logger.LogWarning($"MiDataService {identifier}: Unknown ApplyResult received, skipping further processing...");
                break;
        }
    }

    /// <summary>
    /// Evaluates a BL check response and logs the result.
    /// </summary>
    private static MiEnums.MiAuthApplicationResult EvaluateBlCheckResponse(
        BaseResponse<BlCheckResponseDto> response,
        string identifier)
    {
        if (response.Code == MiDataConstants.STATUS_COOKIE_EXPIRED)
        {
            Logger.LogError($"MiDataService {identifier} - Cookie expired. Please update the cookie in the configuration.");
            throw new InvalidOperationException($"MiDataService {identifier} - Pre-check failed due to expired cookie. Cannot start MiAuthRequestProcessor.");
        }

        Logger.LogInfo($"MiDataService {identifier} - Pre-check successful. is_pass: {response.Data!.IsPass}, button_state: {response.Data.ButtonState}, deadline_format: {response.Data.DeadlineFormat}");

        if (response.Data.MiEnumIsPass == MiEnums.MiIsPassState.Unknown)
        {
            Logger.LogWarning($"MiDataService {identifier} - is_pass state is UNKNOWN. Please verify the account status manually.");
            return MiEnums.MiAuthApplicationResult.Unknown;
        }

        if (response.Data.MiEnumIsPass == MiEnums.MiIsPassState.RequestApproved)
        {
            Logger.LogWarning($"MiDataService {identifier} - Device is NOT eligible for unlocking at this time as request was already approved. Please try and unlock Mi Bootloader on your device.");
            return MiEnums.MiAuthApplicationResult.ApplicationApproved;
        }

        Logger.LogInfo($"MiDataService {identifier} - Device may be eligible for unlocking.");

        return response.Data.MiEnumButtonState switch
        {
            MiEnums.MiButtonState.RequestSubmissionPossible => LogAndReturn(
                $"MiDataService {identifier} - Button State: Request Submission Possible.",
                MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved,
                Logger.LogInfo),

            MiEnums.MiButtonState.AccountBlockedFromApplyingUntilDate => LogAndReturn(
                $"MiDataService {identifier} - Button State: Account Blocked From Applying Until Date.",
                MiEnums.MiAuthApplicationResult.ApplicationRejected,
                Logger.LogInfo),

            MiEnums.MiButtonState.AccountCreatedLessThan30DaysAgo => LogAndReturn(
                $"MiDataService {identifier} - Button State: Account Created Less Than 30 Days Ago.",
                MiEnums.MiAuthApplicationResult.ApplicationRejected,
                Logger.LogInfo),

            _ => LogAndReturn(
                $"MiDataService {identifier} - Button State is UNKNOWN. Please verify the account status manually.",
                MiEnums.MiAuthApplicationResult.Unknown,
                Logger.LogWarning)
        };
    }

    private static MiEnums.MiAuthApplicationResult LogAndReturn(
        string message,
        MiEnums.MiAuthApplicationResult result,
        Action<string, object[]> logAction)
    {
        logAction(message, []);
        return result;
    }

    private static void OnAllThresholdsReached(object? sender, EventArgs e)
    {
        Logger.LogInfo("All clock thresholds have been triggered!");
        _allThresholdsReached = true;
        CheckAllOperationsCompleted();
    }
}
