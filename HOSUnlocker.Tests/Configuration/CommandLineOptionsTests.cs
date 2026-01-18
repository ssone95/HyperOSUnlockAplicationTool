using HOSUnlock.Configuration;

namespace HOSUnlock.Tests.Configuration;

[TestClass]
public sealed class CommandLineOptionsTests
{
    private const int TestTimeoutMs = 360000; // 6 minutes

    #region Default Values Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void NewInstance_AllPropertiesAreNull()
    {
        // Arrange & Act
        var options = new CommandLineOptions();

        // Assert
        Assert.IsNull(options.HeadlessMode);
        Assert.IsNull(options.AutoRunOnStart);
        Assert.IsNull(options.MaxAutoRetries);
        Assert.IsNull(options.MaxApiRetries);
        Assert.IsNull(options.ApiRetryWaitTimeMs);
        Assert.IsNull(options.MultiplyApiRetryWaitTimeByAttempt);
    }

    #endregion

    #region ShouldRunHeadless Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ShouldRunHeadless_WhenHeadlessModeNull_ReturnsFalse()
    {
        // Arrange
        var options = new CommandLineOptions { HeadlessMode = null };

        // Act & Assert
        Assert.IsFalse(options.ShouldRunHeadless);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ShouldRunHeadless_WhenHeadlessModeFalse_ReturnsFalse()
    {
        // Arrange
        var options = new CommandLineOptions { HeadlessMode = false };

        // Act & Assert
        Assert.IsFalse(options.ShouldRunHeadless);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ShouldRunHeadless_WhenHeadlessModeTrue_ReturnsTrue()
    {
        // Arrange
        var options = new CommandLineOptions { HeadlessMode = true };

        // Act & Assert
        Assert.IsTrue(options.ShouldRunHeadless);
    }

    #endregion

    #region ApplyTo Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyTo_WithHeadlessMode_AppliesHeadlessMode()
    {
        // Arrange
        var options = new CommandLineOptions { HeadlessMode = true };
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100],
            HeadlessMode = false
        };

        // Act
        options.ApplyTo(config);

        // Assert
        Assert.IsTrue(config.HeadlessMode);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyTo_WithAutoRunOnStart_AppliesAutoRunOnStart()
    {
        // Arrange
        var options = new CommandLineOptions { AutoRunOnStart = true };
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100],
            AutoRunOnStart = false
        };

        // Act
        options.ApplyTo(config);

        // Assert
        Assert.IsTrue(config.AutoRunOnStart);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyTo_WithNullOptions_DoesNotChangeConfig()
    {
        // Arrange
        var options = new CommandLineOptions(); // All null
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100],
            HeadlessMode = false,
            AutoRunOnStart = false
        };

        // Act
        options.ApplyTo(config);

        // Assert
        Assert.IsFalse(config.HeadlessMode);
        Assert.IsFalse(config.AutoRunOnStart);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyTo_WithAllOptions_AppliesAll()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            HeadlessMode = true,
            AutoRunOnStart = true
        };
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100]
        };

        // Act
        options.ApplyTo(config);

        // Assert
        Assert.IsTrue(config.HeadlessMode);
        Assert.IsTrue(config.AutoRunOnStart);
    }

    #endregion
}
