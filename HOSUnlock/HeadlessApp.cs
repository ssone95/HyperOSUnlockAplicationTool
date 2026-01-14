using HOSUnlock.Configuration;
using HOSUnlock.Constants;
using HOSUnlock.Enums;
using HOSUnlock.Models;
using HOSUnlock.Models.Base;
using HOSUnlock.Models.Common;
using HOSUnlock.Services;

namespace HOSUnlock;

public class HeadlessApp
{
    private static CancellationTokenSource? _cancellationTokenSource;
    private static bool _allThresholdsReached = false;
    private static MiAuthRequestProcessor _miAuthRequestProcessor = null!;

    public static async Task Run(string[] args)
    {
        Logger.InitializeLogger("Headless", true);
        _cancellationTokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Logger.LogInfo("Received interrupt signal. Shutting down...");
            _cancellationTokenSource?.Cancel();
        };

        try
        {
            Logger.LogInfo("HOSUnlock - Headless Mode");
            Logger.LogInfo("Press Ctrl+C to stop");

            await LoadConfiguration();

            if (AppConfiguration.Instance == null || !AppConfiguration.Instance.IsConfigurationValid())
            {
                Logger.LogError("Application configuration is invalid.");
                Logger.LogError("Please check whether the appsettings.json file is valid and present in the same directory as the program.");
                return;
            }

            if (args.Length > 0 && args.Any(y => string.Equals(y, "--auto-run", StringComparison.OrdinalIgnoreCase)))
            {
                AppConfiguration.Instance.AutoRunOnStart = true;
            }

            Logger.LogInfo("Configuration loaded successfully.");

            foreach (var shiftData in AppConfiguration.Instance.TokenShifts.Select((shift, index) => (index: index + 1, shift)))
            {
                Logger.LogInfo($"Token Shift #{shiftData.index}: {shiftData.shift}ms");
            }

            if (!AppConfiguration.Instance.AutoRunOnStart)
            {
                Logger.LogInfo("Auto-run on start is disabled. Press Enter to begin monitoring \nor any other key to exit:");
                var key = Console.ReadKey();
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

            await ClockProvider.Initialize();
            Logger.LogInfo("Clock provider initialized.");

            _miAuthRequestProcessor = new MiAuthRequestProcessor(AppConfiguration.Instance, _cancellationTokenSource);
            Logger.LogInfo("MiAuth request processor started.");
            var preCheckResults = await _miAuthRequestProcessor.Start();

            bool shouldProceed = true;
            foreach (var kvp in preCheckResults)
            {
                var preRunVerificationResult = await VerifyStatusOfService(kvp);
                if (preRunVerificationResult == MiEnums.MiAuthApplicationResult.ApplicationApproved ||
                    preRunVerificationResult == MiEnums.MiAuthApplicationResult.ApplicationRejected)
                {
                    Logger.LogInfo("Since the application is already approved or rejected, its clock threshold will not be monitored.");
                    Logger.LogInfo("Application will exit now.");
                    return;
                }

                if (preRunVerificationResult == MiEnums.MiAuthApplicationResult.Unknown)
                {
                    Logger.LogWarning("Pre-run verification returned UNKNOWN status. Please verify the account status manually.");
                    Logger.LogWarning("Application will exit now.");
                    return;
                }

                shouldProceed = shouldProceed && preRunVerificationResult == MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved;
            }

            if (!shouldProceed)
            {
                Logger.LogWarning("Application cannot proceed due to pre-run verification failures. Please check your account details and logs for more details.");
                return;
            }

            if (!AppConfiguration.Instance.AutoRunOnStart)
            {
                Logger.LogInfo("Press Enter to continue monitoring the clock thresholds \nor any other key to exit:");
                var key = Console.ReadKey();
                if (key.Key != ConsoleKey.Enter)
                {
                    Logger.LogInfo("Non-Enter key pressed. Exiting application.");
                    return;
                }
            }

            // Subscribe to clock events
            ClockProvider.Instance.OnClockThresholdExceeded += OnClockThresholdExceeded;
            ClockProvider.Instance.OnAllThresholdsReached += OnAllThresholdsReached;

            // Display initial threshold times
            await DisplayThresholdTimes();

            Logger.LogInfo("Monitoring clock thresholds...");
            Logger.LogInfo("Application will automatically exit when all thresholds are reached.");

            // Keep the application running until cancellation or all thresholds reached
            while (!_cancellationTokenSource.Token.IsCancellationRequested && !(await ClockProvider.Instance.WereAllThresholdsReached()))
            {
                await Task.Delay(1000, _cancellationTokenSource.Token);

                // Display current time every 30 seconds
                if (DateTime.Now.Second % 30 == 0)
                {
                    var (localTime, utcTime, beijingTime) = await ClockProvider.Instance.GetCurrentTimes();
                    Logger.LogInfo($"Beijing: {beijingTime:yyyy-MM-dd HH:mm:ss.fff} | UTC: {utcTime:yyyy-MM-dd HH:mm:ss.fff} | Local: {localTime:yyyy-MM-dd HH:mm:ss.fff}\n" +
                        $"You can always interrupt the process using Ctrl+C key combination.");
                }
            }

            if (_allThresholdsReached)
            {
                Logger.LogInfo("All thresholds reached! Application completed.");
            }

            if (!AppConfiguration.Instance.AutoRunOnStart)
            {
                Logger.LogInfo("Application process was done, please check the status details and proceed according to instructions.\nPress any key to terminate the application.");
                Console.ReadKey();
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
            // Cleanup
            if (_miAuthRequestProcessor != null)
            {
                await _miAuthRequestProcessor.Stop();
            }

            if (ClockProvider.Instance != null)
            {
                ClockProvider.Instance.OnClockThresholdExceeded -= OnClockThresholdExceeded;
                ClockProvider.Instance.OnAllThresholdsReached -= OnAllThresholdsReached;
                ClockProvider.DisposeInstance();
            }

            Logger.LogInfo("Application shutdown complete.");
            _cancellationTokenSource.Dispose();
            Logger.DisposeLogger();
        }
    }

    private static async Task<MiEnums.MiAuthApplicationResult> VerifyStatusOfService(KeyValuePair<TokenInfo, BaseResponse<BlCheckResponseDto>> kvp)
    {
        if (kvp.Value == null || kvp.Value.Data == null)
        {
            Logger.LogError($"MiDataService Token{kvp.Key} - Failed to retrieve status. Response Code: {kvp.Value?.Code}, Message: {kvp.Value?.Message}");
            throw new InvalidOperationException($"MiDataService Token{kvp.Key} - Pre-check failed. Cannot start MiAuthRequestProcessor.");
        }

        return await ProcessSingleBlCheckResult(kvp.Value, kvp.Key);
    }

    private static async Task DisplayThresholdTimes()
    {
        var thresholds = await ClockProvider.Instance.GetThresholdSnapshots();
        Logger.LogInfo("Threshold times:");

        foreach (var kvp in thresholds.OrderBy(x => x.Key.TokenIndex)
            .ThenBy(x => x.Key.ShiftIndex))
        {
            Logger.LogInfo($"  TokenThreshold Token #{kvp.Key.TokenIndex} Shift #{kvp.Key.ShiftIndex}: Beijing: {kvp.Value.beijing:yyyy-MM-dd HH:mm:ss.fff.fff} | UTC: {kvp.Value.utc:yyyy-MM-dd HH:mm:ss.fff.fff} | Local: {kvp.Value.local:yyyy-MM-dd HH:mm:ss.fff.fff}");
        }
    }

    private static async void OnClockThresholdExceeded(object? sender, ClockProvider.ClockThresholdExceededArgs e)
    {
        try
        {
            var details = e.TokenShiftDetails;

            Logger.LogInfo($"[THRESHOLD] Token{details.TokenIndex} Shift{details.ShiftIndex} threshold exceeded!");
            Logger.LogInfo($"            Beijing Time: {e.BeijingTime:yyyy-MM-dd HH:mm:ss.fff.fff}");
            Logger.LogInfo($"            UTC Time: {e.UtcTime:yyyy-MM-dd HH:mm:ss.fff.fff}");
            Logger.LogInfo($"            Local Time: {e.LocalTime:yyyy-MM-dd HH:mm:ss.fff.fff}");

            var applicationResult = await _miAuthRequestProcessor.ApplyForUnlock(details);

            await ProcessApplicationRequestResult(applicationResult, details);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error in OnClockThresholdExceeded: {0}", ex, ex.Message);
        }
    }

    private static SemaphoreSlim _applicationProcessingSemaphore = new(1, 1);
    private static async Task ProcessApplicationRequestResult(BaseResponse<ApplyBlAuthResponseDto> applicationResult, TokenShiftDefinition shiftDetails)
    {
        if (applicationResult == null || applicationResult.Data == null)
        {
            Logger.LogError($"MiDataService Token{shiftDetails.TokenIndex} Shift {shiftDetails.ShiftIndex} - Failed to retrieve application result. Response Code: {applicationResult?.Code}, Message: {applicationResult?.Message}");
            return;
        }

        await _applicationProcessingSemaphore.WaitAsync();

        try
        {
            var response = applicationResult;

            switch (applicationResult.Code)
            {
                case MiDataConstants.STATUS_CODE_SUCCESS:
                    Logger.LogInfo($"MiDataService Token{shiftDetails.TokenIndex} Shift {shiftDetails.ShiftIndex} - Application details available! Deadline received: {response.Data.DeadlineFormat}");
                    Logger.LogInfo($"MiDataService Token{shiftDetails.TokenIndex} Shift {shiftDetails.ShiftIndex} - Checking the detailed application status...");
                    await RunDetailedApplicationStatusChecks(response, shiftDetails);
                    break;
                case MiDataConstants.STATUS_REQUEST_POTENTIALLY_VALID:
                    Logger.LogWarning($"MiDataService Token{shiftDetails.TokenIndex} Shift {shiftDetails.ShiftIndex} - Application may have already been submitted. Deadline received: {response.Data.DeadlineFormat}");
                    Logger.LogInfo($"MiDataService Token{shiftDetails.TokenIndex} Shift {shiftDetails.ShiftIndex} - Checking the detailed application status...");
                    var result = await _miAuthRequestProcessor.RunSingleBlCheck(shiftDetails.GetTokenInfo());
                    await ProcessSingleBlCheckResult(result, shiftDetails);
                    break;
                case MiDataConstants.STATUS_COOKIE_EXPIRED:
                    Logger.LogError($"MiDataService Token{shiftDetails.TokenIndex} Shift {shiftDetails.ShiftIndex} - Cookie expired. Please update the cookie in the configuration.");
                    break;
                case MiDataConstants.STATUS_REQUEST_REJECTED:
                case MiDataConstants.STATUS_CODE_OTHER_FAILURE:
                    Logger.LogWarning($"MiDataService Token{shiftDetails.TokenIndex} Shift {shiftDetails.ShiftIndex} - Application limit reached. You may only apply once every 30 days.");
                    break;
                default:
                    Logger.LogError($"MiDataService Token{shiftDetails.TokenIndex} Shift {shiftDetails.ShiftIndex} - Unknown response code: {response.Code}. Message: {response.Message}");
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

    private static async Task RunDetailedApplicationStatusChecks(BaseResponse<ApplyBlAuthResponseDto> response, TokenShiftDefinition shiftDefinition)
    {
        switch (response.Data!.ApplyResultState)
        {
            case MiEnums.MiApplyResult.ApplicationSuccessful:
                var blResult = await _miAuthRequestProcessor.RunSingleBlCheck(shiftDefinition.GetTokenInfo());
                var applicationResult = await ProcessSingleBlCheckResult(blResult, shiftDefinition);
                if (applicationResult == MiEnums.MiAuthApplicationResult.ApplicationApproved)
                    Logger.LogInfo($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex}: Unlock request was approved, please try to unlock the boot loader on your device!");
                else if (applicationResult == MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved)
                    Logger.LogInfo($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex}: Unlock request is in an uncertain state, try to unlock the boot loader, if that doesn't work - please try and run the application again for a new approval request.");
                else
                    Logger.LogError($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex}: Unlock request was not approved, please try again at a later time depending on the state of your account.");
                break;
            case MiEnums.MiApplyResult.LimitReached:
                Logger.LogWarning($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex}: Request limit reached, try again on {response.Data.DeadlineFormat ?? "Not Specified"} date (format is Month/Day of the current year).");
                break;
            case MiEnums.MiApplyResult.BlockedUntil:
                Logger.LogError($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex}: Request was blocked, try again on {response.Data.DeadlineFormat ?? "NotSpecified"} date (format is Month/Day of the current year).");
                break;
            default:
                Logger.LogWarning($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex}: Unknown ApplyResult received, skipping further processing...");
                break;
        }
    }
    private static async Task<MiEnums.MiAuthApplicationResult> ProcessSingleBlCheckResult(BaseResponse<BlCheckResponseDto> response, TokenInfo tokenInfo)
    {
        if (response.Code == MiDataConstants.STATUS_COOKIE_EXPIRED)
        {
            Logger.LogError($"MiDataService Token #{tokenInfo.Index} - Cookie expired. Please update the cookie in the configuration.");
            throw new InvalidOperationException($"MiDataService Token #{tokenInfo.Index} - Pre-check failed due to expired cookie. Cannot start MiAuthRequestProcessor.");
        }

        Logger.LogInfo($"MiDataService Token #{tokenInfo.Index} - Pre-check successful. is_pass: {response.Data!.IsPass}, button_state: {response.Data.ButtonState}, deadline_format: {response.Data.DeadlineFormat}");

        if (response.Data.MiEnumIsPass == MiEnums.MiIsPassState.Unknown)
        {
            Logger.LogWarning($"MiDataService Token #{tokenInfo.Index} - is_pass state is UNKNOWN. Please verify the account status manually.");
            return MiEnums.MiAuthApplicationResult.Unknown;
        }
        else if (response.Data.MiEnumIsPass == MiEnums.MiIsPassState.RequestApproved)
        {
            Logger.LogWarning($"MiDataService Token #{tokenInfo.Index} - Device is NOT eligible for unlocking at this time as request was already approved. Please try and unlock Mi Bootloader on your device.");
            return MiEnums.MiAuthApplicationResult.ApplicationApproved;
        }

        Logger.LogInfo($"MiDataService Token #{tokenInfo.Index} - Device may be eligible for unlocking.");

        var miButtonState = response.Data.MiEnumButtonState;
        if (miButtonState == MiEnums.MiButtonState.RequestSubmissionPossible)
        {
            Logger.LogInfo($"MiDataService Token #{tokenInfo.Index} - Button State: Request Submission Possible.");
            return MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved;
        }
        else if (miButtonState == MiEnums.MiButtonState.AccountBlockedFromApplyingUntilDate)
        {
            Logger.LogInfo($"MiDataService Token #{tokenInfo.Index}  - Button State: Account Blocked From Applying Until: ");
            return MiEnums.MiAuthApplicationResult.ApplicationRejected;
        }
        else if (miButtonState == MiEnums.MiButtonState.AccountCreatedLessThan30DaysAgo)
        {
            Logger.LogInfo($"MiDataService Token #{tokenInfo.Index} - Button State: Account Created Less Than 30 Days Ago.");
            return MiEnums.MiAuthApplicationResult.ApplicationRejected;
        }

        Logger.LogWarning($"MiDataService Token #{tokenInfo.Index} - Button State is UNKNOWN. Please verify the account status manually.");
        return MiEnums.MiAuthApplicationResult.Unknown;
    }

    private static async Task<MiEnums.MiAuthApplicationResult> ProcessSingleBlCheckResult(BaseResponse<BlCheckResponseDto> response, TokenShiftDefinition shiftDefinition)
    {
        if (response.Code == MiDataConstants.STATUS_COOKIE_EXPIRED)
        {
            Logger.LogError($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex} - Cookie expired. Please update the cookie in the configuration.");
            throw new InvalidOperationException($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex} - Pre-check failed due to expired cookie. Cannot start MiAuthRequestProcessor.");
        }

        Logger.LogInfo($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex} - Pre-check successful. is_pass: {response.Data!.IsPass}, button_state: {response.Data.ButtonState}, deadline_format: {response.Data.DeadlineFormat}");

        if (response.Data.MiEnumIsPass == MiEnums.MiIsPassState.Unknown)
        {
            Logger.LogWarning($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex} - is_pass state is UNKNOWN. Please verify the account status manually.");
            return MiEnums.MiAuthApplicationResult.Unknown;
        }
        else if (response.Data.MiEnumIsPass == MiEnums.MiIsPassState.RequestApproved)
        {
            Logger.LogWarning($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex} - Device is NOT eligible for unlocking at this time as request was already approved. Please try and unlock Mi Bootloader on your device.");
            return MiEnums.MiAuthApplicationResult.ApplicationApproved;
        }

        Logger.LogInfo($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex} - Device may be eligible for unlocking.");

        var miButtonState = response.Data.MiEnumButtonState;
        if (miButtonState == MiEnums.MiButtonState.RequestSubmissionPossible)
        {
            Logger.LogInfo($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex} - Button State: Request Submission Possible.");
            return MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved;
        }
        else if (miButtonState == MiEnums.MiButtonState.AccountBlockedFromApplyingUntilDate)
        {
            Logger.LogInfo($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex}  - Button State: Account Blocked From Applying Until: ");
            return MiEnums.MiAuthApplicationResult.ApplicationRejected;
        }
        else if (miButtonState == MiEnums.MiButtonState.AccountCreatedLessThan30DaysAgo)
        {
            Logger.LogInfo($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex} - Button State: Account Created Less Than 30 Days Ago.");
            return MiEnums.MiAuthApplicationResult.ApplicationRejected;
        }

        Logger.LogWarning($"MiDataService Token #{shiftDefinition.TokenIndex} Shift #{shiftDefinition.ShiftIndex} - Button State is UNKNOWN. Please verify the account status manually.");
        return MiEnums.MiAuthApplicationResult.Unknown;
    }

    private static async void OnAllThresholdsReached(object? sender, EventArgs e)
    {
        Logger.LogInfo("[COMPLETE] All clock thresholds have been reached!");
        Logger.LogInfo("Waiting for final processing...");

        // Give a moment for any final processing
        await Task.Delay(10000);

        Logger.LogInfo("Final processing complete. Exiting application.");
        _allThresholdsReached = true;
    }

    private static async Task LoadConfiguration()
    {
        await AppConfiguration.Load();
    }
}
