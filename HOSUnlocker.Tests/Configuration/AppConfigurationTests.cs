using HOSUnlock.Configuration;
using HOSUnlock.Constants;
using HOSUnlocker.Tests.Infrastructure;

namespace HOSUnlock.Tests.Configuration;

[TestClass]
public sealed class AppConfigurationTests
{
    private const int TestTimeoutMs = 360000; // 6 minutes

    #region IsConfigurationValid Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void IsConfigurationValid_ValidConfig_ReturnsTrue()
    {
        // Arrange
        var config = TestMocks.CreateValidConfiguration();

        // Act
        var result = config.IsConfigurationValid();

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void IsConfigurationValid_EmptyTokens_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [],
            TokenShifts = [100]
        };

        // Act
        var result = config.IsConfigurationValid();

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void IsConfigurationValid_EmptyTokenShifts_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("ValidToken%2B", 1)],
            TokenShifts = []
        };

        // Act
        var result = config.IsConfigurationValid();

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void IsConfigurationValid_DefaultToken_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo(TokenConstants.DefaultTokenValue, 1)],
            TokenShifts = [100]
        };

        // Act
        var result = config.IsConfigurationValid();

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void IsConfigurationValid_WhitespaceToken_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("   ", 1)],
            TokenShifts = [100]
        };

        // Act
        var result = config.IsConfigurationValid();

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void IsConfigurationValid_TokenIndexZero_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("ValidToken%2B", 0)],
            TokenShifts = [100]
        };

        // Act
        var result = config.IsConfigurationValid();

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void IsConfigurationValid_TokenIndexNegative_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("ValidToken%2B", -1)],
            TokenShifts = [100]
        };

        // Act
        var result = config.IsConfigurationValid();

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void IsConfigurationValid_DuplicateTokenIndices_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens =
            [
                new TokenInfo("Token1%2B", 1),
                new TokenInfo("Token2%2B", 1) // Duplicate index
            ],
            TokenShifts = [100]
        };

        // Act
        var result = config.IsConfigurationValid();

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void IsConfigurationValid_DuplicateShiftValues_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("ValidToken%2B", 1)],
            TokenShifts = [100, 100] // Duplicate shift
        };

        // Act
        var result = config.IsConfigurationValid();

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void IsConfigurationValid_MultipleTokens_ReturnsTrue()
    {
        // Arrange
        var config = TestMocks.CreateValidConfiguration(tokenCount: 3);

        // Act
        var result = config.IsConfigurationValid();

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void IsConfigurationValid_NegativeShiftValues_ReturnsTrue()
    {
        // Arrange - negative shifts are valid
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("ValidToken%2B", 1)],
            TokenShifts = [-100, -200, 100]
        };

        // Act
        var result = config.IsConfigurationValid();

        // Assert
        Assert.IsTrue(result);
    }

    #endregion

    #region Validation Methods Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetValidatedMaxAutoRetries_WithinRange_ReturnsValue()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100],
            MaxAutoRetries = 10
        };

        // Act
        var result = config.GetValidatedMaxAutoRetries();

        // Assert
        Assert.AreEqual(10, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetValidatedMaxAutoRetries_BelowMin_ReturnsMin()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100],
            MaxAutoRetries = 0
        };

        // Act
        var result = config.GetValidatedMaxAutoRetries();

        // Assert
        Assert.AreEqual(AppConfiguration.MinAutoRetries, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetValidatedMaxAutoRetries_AboveMax_ReturnsMax()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100],
            MaxAutoRetries = 500
        };

        // Act
        var result = config.GetValidatedMaxAutoRetries();

        // Assert
        Assert.AreEqual(AppConfiguration.MaxAutoRetriesLimit, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetValidatedMaxApiRetries_WithinRange_ReturnsValue()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100],
            MaxApiRetries = 5
        };

        // Act
        var result = config.GetValidatedMaxApiRetries();

        // Assert
        Assert.AreEqual(5, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetValidatedMaxApiRetries_Zero_ReturnsZero()
    {
        // Arrange - 0 is valid (no retries)
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100],
            MaxApiRetries = 0
        };

        // Act
        var result = config.GetValidatedMaxApiRetries();

        // Assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetValidatedMaxApiRetries_AboveMax_ReturnsMax()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100],
            MaxApiRetries = 20
        };

        // Act
        var result = config.GetValidatedMaxApiRetries();

        // Assert
        Assert.AreEqual(AppConfiguration.MaxApiRetriesLimit, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetValidatedApiRetryWaitTimeMs_WithinRange_ReturnsValue()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100],
            ApiRetryWaitTimeMs = 200
        };

        // Act
        var result = config.GetValidatedApiRetryWaitTimeMs();

        // Assert
        Assert.AreEqual(200, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetValidatedApiRetryWaitTimeMs_BelowMin_ReturnsMin()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100],
            ApiRetryWaitTimeMs = 0
        };

        // Act
        var result = config.GetValidatedApiRetryWaitTimeMs();

        // Assert
        Assert.AreEqual(AppConfiguration.MinApiRetryWaitTimeMs, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetValidatedApiRetryWaitTimeMs_AboveMax_ReturnsMax()
    {
        // Arrange
        var config = new AppConfiguration
        {
            Tokens = [new TokenInfo("Token%2B", 1)],
            TokenShifts = [100],
            ApiRetryWaitTimeMs = 2000
        };

        // Act
        var result = config.GetValidatedApiRetryWaitTimeMs();

        // Assert
        Assert.AreEqual(AppConfiguration.MaxApiRetryWaitTimeMsLimit, result);
    }

    #endregion

    #region ApplyCommandLineOverrides Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyCommandLineOverrides_HeadlessMode_Applied()
    {
        // Arrange
        var config = TestMocks.CreateValidConfiguration();
        var options = new CommandLineOptions { HeadlessMode = true };

        // Act
        config.ApplyCommandLineOverrides(options);

        // Assert
        Assert.IsTrue(config.HeadlessMode);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyCommandLineOverrides_AutoRunOnStart_Applied()
    {
        // Arrange
        var config = TestMocks.CreateValidConfiguration();
        var options = new CommandLineOptions { AutoRunOnStart = true };

        // Act
        config.ApplyCommandLineOverrides(options);

        // Assert
        Assert.IsTrue(config.AutoRunOnStart);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyCommandLineOverrides_MaxAutoRetries_Applied()
    {
        // Arrange
        var config = TestMocks.CreateValidConfiguration();
        var options = new CommandLineOptions { MaxAutoRetries = 25 };

        // Act
        config.ApplyCommandLineOverrides(options);

        // Assert
        Assert.AreEqual(25, config.MaxAutoRetries);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyCommandLineOverrides_MaxApiRetries_Applied()
    {
        // Arrange
        var config = TestMocks.CreateValidConfiguration();
        var options = new CommandLineOptions { MaxApiRetries = 7 };

        // Act
        config.ApplyCommandLineOverrides(options);

        // Assert
        Assert.AreEqual(7, config.MaxApiRetries);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyCommandLineOverrides_ApiRetryWaitTimeMs_Applied()
    {
        // Arrange
        var config = TestMocks.CreateValidConfiguration();
        var options = new CommandLineOptions { ApiRetryWaitTimeMs = 300 };

        // Act
        config.ApplyCommandLineOverrides(options);

        // Assert
        Assert.AreEqual(300, config.ApiRetryWaitTimeMs);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyCommandLineOverrides_MultiplyApiRetryWaitTimeByAttempt_Applied()
    {
        // Arrange
        var config = TestMocks.CreateValidConfiguration();
        var options = new CommandLineOptions { MultiplyApiRetryWaitTimeByAttempt = false };

        // Act
        config.ApplyCommandLineOverrides(options);

        // Assert
        Assert.IsFalse(config.MultiplyApiRetryWaitTimeByAttempt);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyCommandLineOverrides_NullOptions_NoChanges()
    {
        // Arrange
        var config = TestMocks.CreateValidConfiguration();
        config.MaxAutoRetries = 5;
        config.MaxApiRetries = 3;
        var options = new CommandLineOptions(); // All null

        // Act
        config.ApplyCommandLineOverrides(options);

        // Assert - values unchanged
        Assert.AreEqual(5, config.MaxAutoRetries);
        Assert.AreEqual(3, config.MaxApiRetries);
        Assert.IsFalse(config.HeadlessMode);
        Assert.IsFalse(config.AutoRunOnStart);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ApplyCommandLineOverrides_AllOptions_AllApplied()
    {
        // Arrange
        var config = TestMocks.CreateValidConfiguration();
        var options = new CommandLineOptions
        {
            HeadlessMode = true,
            AutoRunOnStart = true,
            MaxAutoRetries = 50,
            MaxApiRetries = 8,
            ApiRetryWaitTimeMs = 500,
            MultiplyApiRetryWaitTimeByAttempt = false
        };

        // Act
        config.ApplyCommandLineOverrides(options);

        // Assert
        Assert.IsTrue(config.HeadlessMode);
        Assert.IsTrue(config.AutoRunOnStart);
        Assert.AreEqual(50, config.MaxAutoRetries);
        Assert.AreEqual(8, config.MaxApiRetries);
        Assert.AreEqual(500, config.ApiRetryWaitTimeMs);
        Assert.IsFalse(config.MultiplyApiRetryWaitTimeByAttempt);
    }

    #endregion

    #region LoadDefault Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void LoadDefault_ReturnsConfigWithDefaultToken()
    {
        // Act
        var config = AppConfiguration.LoadDefault();

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual(1, config.Tokens.Length);
        Assert.AreEqual(TokenConstants.DefaultTokenValue, config.Tokens[0].Token);
        Assert.AreEqual(1, config.Tokens[0].Index);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void LoadDefault_ReturnsConfigWithDefaultShifts()
    {
        // Act
        var config = AppConfiguration.LoadDefault();

        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual(1, config.TokenShifts.Length);
        Assert.AreEqual(0, config.TokenShifts[0]);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void LoadDefault_ConfigIsNotValid()
    {
        // Act
        var config = AppConfiguration.LoadDefault();

        // Assert - default config should not be valid (uses placeholder token)
        Assert.IsFalse(config.IsConfigurationValid());
    }

    #endregion

    #region Constants Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constants_DefaultMaxAutoRetries_IsFive()
    {
        Assert.AreEqual(5, AppConfiguration.DefaultMaxAutoRetries);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constants_MinAutoRetries_IsOne()
    {
        Assert.AreEqual(1, AppConfiguration.MinAutoRetries);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constants_MaxAutoRetriesLimit_Is365()
    {
        Assert.AreEqual(365, AppConfiguration.MaxAutoRetriesLimit);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constants_DefaultMaxApiRetries_IsThree()
    {
        Assert.AreEqual(3, AppConfiguration.DefaultMaxApiRetries);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constants_MinApiRetries_IsZero()
    {
        Assert.AreEqual(0, AppConfiguration.MinApiRetries);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constants_MaxApiRetriesLimit_IsTen()
    {
        Assert.AreEqual(10, AppConfiguration.MaxApiRetriesLimit);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constants_DefaultApiRetryWaitTimeMs_Is100()
    {
        Assert.AreEqual(100, AppConfiguration.DefaultApiRetryWaitTimeMs);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constants_MinApiRetryWaitTimeMs_IsOne()
    {
        Assert.AreEqual(1, AppConfiguration.MinApiRetryWaitTimeMs);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constants_MaxApiRetryWaitTimeMsLimit_Is1000()
    {
        Assert.AreEqual(1000, AppConfiguration.MaxApiRetryWaitTimeMsLimit);
    }

    #endregion
}
