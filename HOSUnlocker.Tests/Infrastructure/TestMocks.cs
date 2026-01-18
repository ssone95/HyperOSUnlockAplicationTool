using HOSUnlock.Configuration;
using HOSUnlock.Constants;
using HOSUnlock.Enums;
using HOSUnlock.Models;
using HOSUnlock.Models.Base;
using HOSUnlock.Models.Common;
using HOSUnlock.Services;
using Moq;

namespace HOSUnlocker.Tests.Infrastructure;

/// <summary>
/// Provides mock setups and test data builders for unit tests.
/// Each instance is independent, allowing parallel test execution.
/// </summary>
public sealed class TestMocks
{
    public Mock<IMiDataService> MiDataService { get; } = new();
    public Mock<IMiAuthRequestProcessor> MiAuthRequestProcessor { get; } = new();
    public Mock<IClockProvider> ClockProvider { get; } = new();
    public Mock<ILogger> Logger { get; } = new();
    public Mock<IMiDataServiceFactory> MiDataServiceFactory { get; } = new();

    /// <summary>
    /// Creates a valid test configuration with specified tokens and shifts.
    /// </summary>
    public static AppConfiguration CreateValidConfiguration(
        int tokenCount = 1,
        int[]? tokenShifts = null)
    {
        var tokens = Enumerable.Range(1, tokenCount)
            .Select(i => new TokenInfo($"ValidToken{i}%2BEncoded%3D", i))
            .ToArray();

        return new AppConfiguration
        {
            Tokens = tokens,
            TokenShifts = tokenShifts ?? [100, -100]
        };
    }

    /// <summary>
    /// Creates an invalid test configuration.
    /// </summary>
    public static AppConfiguration CreateInvalidConfiguration()
    {
        return new AppConfiguration
        {
            Tokens = [new TokenInfo(TokenConstants.DefaultTokenValue, 1)],
            TokenShifts = [0]
        };
    }

    /// <summary>
    /// Creates a successful BL check response indicating approval.
    /// </summary>
    public static BaseResponse<BlCheckResponseDto> CreateApprovedBlCheckResponse()
    {
        return new BaseResponse<BlCheckResponseDto>
        {
            Code = MiDataConstants.STATUS_CODE_SUCCESS,
            Message = "Success",
            Data = new BlCheckResponseDto
            {
                IsPass = (int)MiEnums.MiIsPassState.RequestApproved,
                ButtonState = (int)MiEnums.MiButtonState.RequestSubmissionPossible,
                DeadlineFormat = "01/15"
            }
        };
    }

    /// <summary>
    /// Creates a BL check response indicating eligibility for unlock request.
    /// </summary>
    public static BaseResponse<BlCheckResponseDto> CreateEligibleBlCheckResponse()
    {
        return new BaseResponse<BlCheckResponseDto>
        {
            Code = MiDataConstants.STATUS_CODE_SUCCESS,
            Message = "Success",
            Data = new BlCheckResponseDto
            {
                IsPass = (int)MiEnums.MiIsPassState.MaybeCanProceed,
                ButtonState = (int)MiEnums.MiButtonState.RequestSubmissionPossible,
                DeadlineFormat = null
            }
        };
    }

    /// <summary>
    /// Creates a BL check response indicating account is blocked.
    /// </summary>
    public static BaseResponse<BlCheckResponseDto> CreateBlockedBlCheckResponse()
    {
        return new BaseResponse<BlCheckResponseDto>
        {
            Code = MiDataConstants.STATUS_CODE_SUCCESS,
            Message = "Success",
            Data = new BlCheckResponseDto
            {
                IsPass = (int)MiEnums.MiIsPassState.MaybeCanProceed,
                ButtonState = (int)MiEnums.MiButtonState.AccountBlockedFromApplyingUntilDate,
                DeadlineFormat = "02/20"
            }
        };
    }

    /// <summary>
    /// Creates a BL check response indicating account created less than 30 days ago.
    /// </summary>
    public static BaseResponse<BlCheckResponseDto> CreateNewAccountBlCheckResponse()
    {
        return new BaseResponse<BlCheckResponseDto>
        {
            Code = MiDataConstants.STATUS_CODE_SUCCESS,
            Message = "Success",
            Data = new BlCheckResponseDto
            {
                IsPass = (int)MiEnums.MiIsPassState.MaybeCanProceed,
                ButtonState = (int)MiEnums.MiButtonState.AccountCreatedLessThan30DaysAgo,
                DeadlineFormat = null
            }
        };
    }

    /// <summary>
    /// Creates a BL check response with unknown is_pass state.
    /// </summary>
    public static BaseResponse<BlCheckResponseDto> CreateUnknownIsPassBlCheckResponse()
    {
        return new BaseResponse<BlCheckResponseDto>
        {
            Code = MiDataConstants.STATUS_CODE_SUCCESS,
            Message = "Success",
            Data = new BlCheckResponseDto
            {
                IsPass = 999, // Unknown value
                ButtonState = (int)MiEnums.MiButtonState.RequestSubmissionPossible,
                DeadlineFormat = null
            }
        };
    }

    /// <summary>
    /// Creates a BL check response with unknown button state.
    /// </summary>
    public static BaseResponse<BlCheckResponseDto> CreateUnknownButtonStateBlCheckResponse()
    {
        return new BaseResponse<BlCheckResponseDto>
        {
            Code = MiDataConstants.STATUS_CODE_SUCCESS,
            Message = "Success",
            Data = new BlCheckResponseDto
            {
                IsPass = (int)MiEnums.MiIsPassState.MaybeCanProceed,
                ButtonState = 999, // Unknown value
                DeadlineFormat = null
            }
        };
    }

    /// <summary>
    /// Creates a BL check response with cookie expired error.
    /// </summary>
    public static BaseResponse<BlCheckResponseDto> CreateCookieExpiredResponse()
    {
        return new BaseResponse<BlCheckResponseDto>
        {
            Code = MiDataConstants.STATUS_COOKIE_EXPIRED,
            Message = "Cookie expired",
            Data = null
        };
    }

    /// <summary>
    /// Creates a successful apply for unlock response.
    /// </summary>
    public static BaseResponse<ApplyBlAuthResponseDto> CreateSuccessfulApplyResponse()
    {
        return new BaseResponse<ApplyBlAuthResponseDto>
        {
            Code = MiDataConstants.STATUS_CODE_SUCCESS,
            Message = "Success",
            Data = new ApplyBlAuthResponseDto
            {
                ApplyResult = (int)MiEnums.MiApplyResult.ApplicationSuccessful,
                DeadlineFormat = "01/20"
            }
        };
    }

    /// <summary>
    /// Creates an apply response indicating limit reached.
    /// </summary>
    public static BaseResponse<ApplyBlAuthResponseDto> CreateLimitReachedApplyResponse()
    {
        return new BaseResponse<ApplyBlAuthResponseDto>
        {
            Code = MiDataConstants.STATUS_CODE_SUCCESS,
            Message = "Success",
            Data = new ApplyBlAuthResponseDto
            {
                ApplyResult = (int)MiEnums.MiApplyResult.LimitReached,
                DeadlineFormat = "02/15"
            }
        };
    }

    /// <summary>
    /// Creates an apply response indicating blocked until date.
    /// </summary>
    public static BaseResponse<ApplyBlAuthResponseDto> CreateBlockedUntilApplyResponse()
    {
        return new BaseResponse<ApplyBlAuthResponseDto>
        {
            Code = MiDataConstants.STATUS_CODE_SUCCESS,
            Message = "Success",
            Data = new ApplyBlAuthResponseDto
            {
                ApplyResult = (int)MiEnums.MiApplyResult.BlockedUntil,
                DeadlineFormat = "03/01"
            }
        };
    }

    /// <summary>
    /// Creates an apply response with request rejected status.
    /// </summary>
    public static BaseResponse<ApplyBlAuthResponseDto> CreateRejectedApplyResponse()
    {
        return new BaseResponse<ApplyBlAuthResponseDto>
        {
            Code = MiDataConstants.STATUS_REQUEST_REJECTED,
            Message = "Request rejected",
            Data = new ApplyBlAuthResponseDto
            {
                ApplyResult = null,
                DeadlineFormat = null
            }
        };
    }

    /// <summary>
    /// Creates an apply response with potentially valid status.
    /// </summary>
    public static BaseResponse<ApplyBlAuthResponseDto> CreatePotentiallyValidApplyResponse()
    {
        return new BaseResponse<ApplyBlAuthResponseDto>
        {
            Code = MiDataConstants.STATUS_REQUEST_POTENTIALLY_VALID,
            Message = "Potentially valid",
            Data = new ApplyBlAuthResponseDto
            {
                ApplyResult = null,
                DeadlineFormat = "01/25"
            }
        };
    }

    /// <summary>
    /// Creates an apply response with cookie expired status.
    /// </summary>
    public static BaseResponse<ApplyBlAuthResponseDto> CreateCookieExpiredApplyResponse()
    {
        return new BaseResponse<ApplyBlAuthResponseDto>
        {
            Code = MiDataConstants.STATUS_COOKIE_EXPIRED,
            Message = "Cookie expired",
            Data = new ApplyBlAuthResponseDto
            {
                ApplyResult = null,
                DeadlineFormat = null
            }
        };
    }

    #region MiDataService Setup Methods

    /// <summary>
    /// Sets up MiDataService to return a successful BL check response.
    /// </summary>
    public TestMocks WithSuccessfulBlCheck()
    {
        MiDataService
            .Setup(x => x.GetStatusCheckResultAsync())
            .ReturnsAsync(CreateEligibleBlCheckResponse());
        return this;
    }

    /// <summary>
    /// Sets up MiDataService to return an approved BL check response.
    /// </summary>
    public TestMocks WithApprovedBlCheck()
    {
        MiDataService
            .Setup(x => x.GetStatusCheckResultAsync())
            .ReturnsAsync(CreateApprovedBlCheckResponse());
        return this;
    }

    /// <summary>
    /// Sets up MiDataService to return a blocked BL check response.
    /// </summary>
    public TestMocks WithBlockedBlCheck()
    {
        MiDataService
            .Setup(x => x.GetStatusCheckResultAsync())
            .ReturnsAsync(CreateBlockedBlCheckResponse());
        return this;
    }

    /// <summary>
    /// Sets up MiDataService to return cookie expired error.
    /// </summary>
    public TestMocks WithCookieExpired()
    {
        MiDataService
            .Setup(x => x.GetStatusCheckResultAsync())
            .ReturnsAsync(CreateCookieExpiredResponse());
        return this;
    }

    /// <summary>
    /// Sets up MiDataService to return a successful apply response.
    /// </summary>
    public TestMocks WithSuccessfulApply()
    {
        MiDataService
            .Setup(x => x.GetApplyAuthForUnlockResultAsync())
            .ReturnsAsync(CreateSuccessfulApplyResponse());
        return this;
    }

    /// <summary>
    /// Sets up MiDataService to return a limit reached apply response.
    /// </summary>
    public TestMocks WithLimitReachedApply()
    {
        MiDataService
            .Setup(x => x.GetApplyAuthForUnlockResultAsync())
            .ReturnsAsync(CreateLimitReachedApplyResponse());
        return this;
    }

    #endregion

    #region MiAuthRequestProcessor Setup Methods

    /// <summary>
    /// Sets up MiAuthRequestProcessor to return successful pre-check results.
    /// </summary>
    public TestMocks WithSuccessfulPreCheck(int tokenCount = 1)
    {
        var results = new Dictionary<TokenInfo, BaseResponse<BlCheckResponseDto>>();
        for (int i = 1; i <= tokenCount; i++)
        {
            results.Add(
                new TokenInfo($"Token{i}%2B", i),
                CreateEligibleBlCheckResponse());
        }

        MiAuthRequestProcessor
            .Setup(x => x.StartAsync())
            .ReturnsAsync(results);
        return this;
    }

    /// <summary>
    /// Sets up MiAuthRequestProcessor to return approved pre-check results.
    /// </summary>
    public TestMocks WithApprovedPreCheck(int tokenCount = 1)
    {
        var results = new Dictionary<TokenInfo, BaseResponse<BlCheckResponseDto>>();
        for (int i = 1; i <= tokenCount; i++)
        {
            results.Add(
                new TokenInfo($"Token{i}%2B", i),
                CreateApprovedBlCheckResponse());
        }

        MiAuthRequestProcessor
            .Setup(x => x.StartAsync())
            .ReturnsAsync(results);
        return this;
    }

    /// <summary>
    /// Sets up MiAuthRequestProcessor to return successful apply result.
    /// </summary>
    public TestMocks WithSuccessfulApplyForUnlock()
    {
        MiAuthRequestProcessor
            .Setup(x => x.ApplyForUnlockAsync(It.IsAny<TokenShiftDefinition>()))
            .ReturnsAsync(CreateSuccessfulApplyResponse());
        return this;
    }

    /// <summary>
    /// Sets up MiAuthRequestProcessor to return limit reached apply result.
    /// </summary>
    public TestMocks WithLimitReachedApplyForUnlock()
    {
        MiAuthRequestProcessor
            .Setup(x => x.ApplyForUnlockAsync(It.IsAny<TokenShiftDefinition>()))
            .ReturnsAsync(CreateLimitReachedApplyResponse());
        return this;
    }

    /// <summary>
    /// Sets up MiDataServiceFactory to return the mock MiDataService.
    /// </summary>
    public TestMocks WithMockDataServiceFactory()
    {
        MiDataServiceFactory
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MiDataService.Object);
        return this;
    }

    #endregion

    #region ClockProvider Setup Methods

    /// <summary>
    /// Sets up ClockProvider with default time values.
    /// </summary>
    public TestMocks WithClockProviderDefaults()
    {
        var utcNow = DateTime.UtcNow;
        var beijingNow = utcNow.AddHours(8);

        ClockProvider.Setup(x => x.UtcNow).Returns(utcNow);
        ClockProvider.Setup(x => x.BeijingNow).Returns(beijingNow);
        ClockProvider.Setup(x => x.LocalNow).Returns(DateTime.Now);
        ClockProvider.Setup(x => x.AttemptCount).Returns(0);
        ClockProvider.Setup(x => x.RemainingAttempts).Returns(5);
        ClockProvider.Setup(x => x.CanRetry).Returns(true);
        ClockProvider.Setup(x => x.MaxRetryAttempts).Returns(5);

        return this;
    }

    /// <summary>
    /// Sets up ClockProvider with specific attempt count.
    /// </summary>
    public TestMocks WithAttemptCount(int attemptCount, int maxRetries = 5)
    {
        ClockProvider.Setup(x => x.AttemptCount).Returns(attemptCount);
        ClockProvider.Setup(x => x.RemainingAttempts).Returns(maxRetries - attemptCount);
        ClockProvider.Setup(x => x.CanRetry).Returns(attemptCount < maxRetries);
        ClockProvider.Setup(x => x.MaxRetryAttempts).Returns(maxRetries);

        return this;
    }

    /// <summary>
    /// Sets up ClockProvider to indicate all thresholds reached.
    /// </summary>
    public TestMocks WithAllThresholdsReached()
    {
        ClockProvider
            .Setup(x => x.WereAllThresholdsReachedAsync())
            .ReturnsAsync(true);
        return this;
    }

    /// <summary>
    /// Sets up ClockProvider to indicate thresholds not yet reached.
    /// </summary>
    public TestMocks WithThresholdsNotReached()
    {
        ClockProvider
            .Setup(x => x.WereAllThresholdsReachedAsync())
            .ReturnsAsync(false);
        return this;
    }

    /// <summary>
    /// Sets up ClockProvider with threshold snapshots.
    /// </summary>
    public TestMocks WithThresholdSnapshots(int tokenCount = 1, int shiftCount = 2)
    {
        var snapshots = new Dictionary<TokenShiftDefinition, (DateTime Beijing, DateTime Utc, DateTime Local)>();
        var baseTime = DateTime.UtcNow.Date.AddHours(16); // 00:00 Beijing time

        for (int t = 1; t <= tokenCount; t++)
        {
            for (int s = 1; s <= shiftCount; s++)
            {
                var shift = s * 100;
                var definition = new TokenShiftDefinition(t, $"Token{t}%2B", s, shift);
                var utcTime = baseTime.AddMilliseconds(-shift);
                var beijingTime = utcTime.AddHours(8);
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local);

                snapshots.Add(definition, (beijingTime, utcTime, localTime));
            }
        }

        ClockProvider
            .Setup(x => x.GetThresholdSnapshots())
            .Returns(snapshots);
        return this;
    }

    /// <summary>
    /// Sets up ClockProvider.ResetAndRestartAsync to succeed.
    /// </summary>
    public TestMocks WithSuccessfulReset()
    {
        ClockProvider
            .Setup(x => x.ResetAndRestartAsync())
            .ReturnsAsync(true);
        return this;
    }

    /// <summary>
    /// Sets up ClockProvider.ResetAndRestartAsync to fail.
    /// </summary>
    public TestMocks WithFailedReset()
    {
        ClockProvider
            .Setup(x => x.ResetAndRestartAsync())
            .ReturnsAsync(false);
        return this;
    }

    #endregion

    #region Verification Methods

    /// <summary>
    /// Verifies that GetStatusCheckResultAsync was called the expected number of times.
    /// </summary>
    public void VerifyBlCheckCalled(Times times)
    {
        MiDataService.Verify(x => x.GetStatusCheckResultAsync(), times);
    }

    /// <summary>
    /// Verifies that GetApplyAuthForUnlockResultAsync was called the expected number of times.
    /// </summary>
    public void VerifyApplyCalled(Times times)
    {
        MiDataService.Verify(x => x.GetApplyAuthForUnlockResultAsync(), times);
    }

    /// <summary>
    /// Verifies that MiAuthRequestProcessor.StartAsync was called.
    /// </summary>
    public void VerifyPreCheckCalled(Times times)
    {
        MiAuthRequestProcessor.Verify(x => x.StartAsync(), times);
    }

    /// <summary>
    /// Verifies that MiAuthRequestProcessor.ApplyForUnlockAsync was called.
    /// </summary>
    public void VerifyApplyForUnlockCalled(Times times)
    {
        MiAuthRequestProcessor.Verify(x => x.ApplyForUnlockAsync(It.IsAny<TokenShiftDefinition>()), times);
    }

    /// <summary>
    /// Verifies that ClockProvider.ResetAndRestartAsync was called.
    /// </summary>
    public void VerifyResetCalled(Times times)
    {
        ClockProvider.Verify(x => x.ResetAndRestartAsync(), times);
    }

    /// <summary>
    /// Verifies that ClockProvider.Stop was called.
    /// </summary>
    public void VerifyStopCalled(Times times)
    {
        ClockProvider.Verify(x => x.Stop(), times);
    }

    /// <summary>
    /// Verifies that ClockProvider.Resume was called.
    /// </summary>
    public void VerifyResumeCalled(Times times)
    {
        ClockProvider.Verify(x => x.Resume(), times);
    }

    /// <summary>
    /// Verifies that MiDataServiceFactory.Create was called.
    /// </summary>
    public void VerifyDataServiceFactoryCreateCalled(Times times)
    {
        MiDataServiceFactory.Verify(x => x.Create(It.IsAny<string>(), It.IsAny<CancellationToken>()), times);
    }

    #endregion
}
