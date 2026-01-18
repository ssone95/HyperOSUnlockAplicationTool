using HOSUnlock.Enums;
using HOSUnlock.Models;

namespace HOSUnlock.Tests.Models;

[TestClass]
public sealed class BlCheckResponseDtoTests
{
    private const int TestTimeoutMs = 360000; // 6 minutes

    #region MiEnumIsPass Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void MiEnumIsPass_RequestApproved_ReturnsCorrectEnum()
    {
        // Arrange
        var dto = new BlCheckResponseDto
        {
            IsPass = (int)MiEnums.MiIsPassState.RequestApproved,
            ButtonState = 1
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiIsPassState.RequestApproved, dto.MiEnumIsPass);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void MiEnumIsPass_MaybeCanProceed_ReturnsCorrectEnum()
    {
        // Arrange
        var dto = new BlCheckResponseDto
        {
            IsPass = (int)MiEnums.MiIsPassState.MaybeCanProceed,
            ButtonState = 1
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiIsPassState.MaybeCanProceed, dto.MiEnumIsPass);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void MiEnumIsPass_UnknownValue_ReturnsUnknown()
    {
        // Arrange
        var dto = new BlCheckResponseDto
        {
            IsPass = 999, // Unknown value
            ButtonState = 1
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiIsPassState.Unknown, dto.MiEnumIsPass);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void MiEnumIsPass_NegativeUnknownValue_ReturnsUnknown()
    {
        // Arrange
        var dto = new BlCheckResponseDto
        {
            IsPass = -999,
            ButtonState = 1
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiIsPassState.Unknown, dto.MiEnumIsPass);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void MiEnumIsPass_Zero_ReturnsUnknown()
    {
        // Arrange
        var dto = new BlCheckResponseDto
        {
            IsPass = 0,
            ButtonState = 1
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiIsPassState.Unknown, dto.MiEnumIsPass);
    }

    #endregion

    #region MiEnumButtonState Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void MiEnumButtonState_RequestSubmissionPossible_ReturnsCorrectEnum()
    {
        // Arrange
        var dto = new BlCheckResponseDto
        {
            IsPass = 1,
            ButtonState = (int)MiEnums.MiButtonState.RequestSubmissionPossible
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiButtonState.RequestSubmissionPossible, dto.MiEnumButtonState);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void MiEnumButtonState_AccountBlockedFromApplyingUntilDate_ReturnsCorrectEnum()
    {
        // Arrange
        var dto = new BlCheckResponseDto
        {
            IsPass = 1,
            ButtonState = (int)MiEnums.MiButtonState.AccountBlockedFromApplyingUntilDate
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiButtonState.AccountBlockedFromApplyingUntilDate, dto.MiEnumButtonState);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void MiEnumButtonState_AccountCreatedLessThan30DaysAgo_ReturnsCorrectEnum()
    {
        // Arrange
        var dto = new BlCheckResponseDto
        {
            IsPass = 1,
            ButtonState = (int)MiEnums.MiButtonState.AccountCreatedLessThan30DaysAgo
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiButtonState.AccountCreatedLessThan30DaysAgo, dto.MiEnumButtonState);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void MiEnumButtonState_UnknownValue_ReturnsUnknown()
    {
        // Arrange
        var dto = new BlCheckResponseDto
        {
            IsPass = 1,
            ButtonState = 999 // Unknown value
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiButtonState.Unknown, dto.MiEnumButtonState);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void MiEnumButtonState_Zero_ReturnsUnknown()
    {
        // Arrange
        var dto = new BlCheckResponseDto
        {
            IsPass = 1,
            ButtonState = 0
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiButtonState.Unknown, dto.MiEnumButtonState);
    }

    #endregion

    #region DeadlineFormat Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void DeadlineFormat_WhenSet_ReturnsValue()
    {
        // Arrange
        var dto = new BlCheckResponseDto
        {
            IsPass = 1,
            ButtonState = 1,
            DeadlineFormat = "01/15"
        };

        // Act & Assert
        Assert.AreEqual("01/15", dto.DeadlineFormat);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void DeadlineFormat_WhenNotSet_ReturnsNull()
    {
        // Arrange
        var dto = new BlCheckResponseDto
        {
            IsPass = 1,
            ButtonState = 1
        };

        // Act & Assert
        Assert.IsNull(dto.DeadlineFormat);
    }

    #endregion
}

[TestClass]
public sealed class ApplyBlAuthResponseDtoTests
{
    private const int TestTimeoutMs = 360000; // 6 minutes

    #region ApplyResultState Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyResultState_ApplicationSuccessful_ReturnsCorrectEnum()
    {
        // Arrange
        var dto = new ApplyBlAuthResponseDto
        {
            ApplyResult = (int)MiEnums.MiApplyResult.ApplicationSuccessful
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiApplyResult.ApplicationSuccessful, dto.ApplyResultState);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyResultState_LimitReached_ReturnsCorrectEnum()
    {
        // Arrange
        var dto = new ApplyBlAuthResponseDto
        {
            ApplyResult = (int)MiEnums.MiApplyResult.LimitReached
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiApplyResult.LimitReached, dto.ApplyResultState);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyResultState_BlockedUntil_ReturnsCorrectEnum()
    {
        // Arrange
        var dto = new ApplyBlAuthResponseDto
        {
            ApplyResult = (int)MiEnums.MiApplyResult.BlockedUntil
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiApplyResult.BlockedUntil, dto.ApplyResultState);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyResultState_Null_ReturnsUnknown()
    {
        // Arrange
        var dto = new ApplyBlAuthResponseDto
        {
            ApplyResult = null
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiApplyResult.Unknown, dto.ApplyResultState);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyResultState_UnknownValue_ReturnsUnknown()
    {
        // Arrange
        var dto = new ApplyBlAuthResponseDto
        {
            ApplyResult = 999 // Unknown value
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiApplyResult.Unknown, dto.ApplyResultState);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyResultState_Zero_ReturnsUnknown()
    {
        // Arrange
        var dto = new ApplyBlAuthResponseDto
        {
            ApplyResult = 0
        };

        // Act & Assert
        Assert.AreEqual(MiEnums.MiApplyResult.Unknown, dto.ApplyResultState);
    }

    #endregion

    #region DeadlineFormat Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void DeadlineFormat_WhenSet_ReturnsValue()
    {
        // Arrange
        var dto = new ApplyBlAuthResponseDto
        {
            ApplyResult = 1,
            DeadlineFormat = "02/20"
        };

        // Act & Assert
        Assert.AreEqual("02/20", dto.DeadlineFormat);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void DeadlineFormat_WhenNotSet_ReturnsNull()
    {
        // Arrange
        var dto = new ApplyBlAuthResponseDto
        {
            ApplyResult = 1
        };

        // Act & Assert
        Assert.IsNull(dto.DeadlineFormat);
    }

    #endregion
}
