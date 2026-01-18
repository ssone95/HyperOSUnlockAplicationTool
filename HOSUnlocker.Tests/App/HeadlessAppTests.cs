using HOSUnlock.Enums;
using HOSUnlocker.Tests.Infrastructure;

namespace HOSUnlock.Tests.App;

[TestClass]
public sealed class HeadlessAppTests
{
    private const int TestTimeoutMs = 360000; // 6 minutes

    #region EvaluateBlCheckResponse Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void EvaluateBlCheckResponse_WithCookieExpired_ThrowsInvalidOperationException()
    {
        // Arrange
        var response = TestMocks.CreateCookieExpiredResponse();
        var identifier = "Token #1";
        Exception? caught = null;

        // Act
        try
        {
            HeadlessApp.EvaluateBlCheckResponse(response, identifier);
        }
        catch (InvalidOperationException ex)
        {
            caught = ex;
        }

        // Assert
        Assert.IsNotNull(caught);
        Assert.IsTrue(caught.Message.Contains("Cookie expired") || caught.Message.Contains("expired cookie"));
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void EvaluateBlCheckResponse_WithApproved_ReturnsApplicationApproved()
    {
        // Arrange
        var response = TestMocks.CreateApprovedBlCheckResponse();
        var identifier = "Token #1";

        // Act
        var result = HeadlessApp.EvaluateBlCheckResponse(response, identifier);

        // Assert
        Assert.AreEqual(MiEnums.MiAuthApplicationResult.ApplicationApproved, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void EvaluateBlCheckResponse_WithEligible_ReturnsApplicationMaybeApproved()
    {
        // Arrange
        var response = TestMocks.CreateEligibleBlCheckResponse();
        var identifier = "Token #1";

        // Act
        var result = HeadlessApp.EvaluateBlCheckResponse(response, identifier);

        // Assert
        Assert.AreEqual(MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void EvaluateBlCheckResponse_WithBlocked_ReturnsApplicationRejected()
    {
        // Arrange
        var response = TestMocks.CreateBlockedBlCheckResponse();
        var identifier = "Token #1";

        // Act
        var result = HeadlessApp.EvaluateBlCheckResponse(response, identifier);

        // Assert
        Assert.AreEqual(MiEnums.MiAuthApplicationResult.ApplicationRejected, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void EvaluateBlCheckResponse_WithNewAccount_ReturnsApplicationRejected()
    {
        // Arrange
        var response = TestMocks.CreateNewAccountBlCheckResponse();
        var identifier = "Token #1";

        // Act
        var result = HeadlessApp.EvaluateBlCheckResponse(response, identifier);

        // Assert
        Assert.AreEqual(MiEnums.MiAuthApplicationResult.ApplicationRejected, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void EvaluateBlCheckResponse_WithUnknownIsPass_ReturnsUnknown()
    {
        // Arrange
        var response = TestMocks.CreateUnknownIsPassBlCheckResponse();
        var identifier = "Token #1";

        // Act
        var result = HeadlessApp.EvaluateBlCheckResponse(response, identifier);

        // Assert
        Assert.AreEqual(MiEnums.MiAuthApplicationResult.Unknown, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void EvaluateBlCheckResponse_WithUnknownButtonState_ReturnsUnknown()
    {
        // Arrange
        var response = TestMocks.CreateUnknownButtonStateBlCheckResponse();
        var identifier = "Token #1";

        // Act
        var result = HeadlessApp.EvaluateBlCheckResponse(response, identifier);

        // Assert
        Assert.AreEqual(MiEnums.MiAuthApplicationResult.Unknown, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void EvaluateBlCheckResponse_WithDifferentIdentifiers_WorksCorrectly()
    {
        // Arrange
        var response = TestMocks.CreateEligibleBlCheckResponse();
        var identifiers = new[] { "Token #1", "Token #2 Shift #3", "Custom Identifier" };

        // Act & Assert
        foreach (var identifier in identifiers)
        {
            var result = HeadlessApp.EvaluateBlCheckResponse(response, identifier);
            Assert.AreEqual(MiEnums.MiAuthApplicationResult.ApplicationMaybeApproved, result);
        }
    }

    #endregion
}
