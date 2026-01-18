using HOSUnlock.Configuration;

namespace HOSUnlocker.Tests.Configuration;

[TestClass]
public sealed class CommandLineParserTests
{
    private const int TestTimeoutMs = 360000; // 6 minutes

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_EmptyArgs_ReturnsSuccessWithDefaults()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out var errorMessage);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNull(errorMessage);
        Assert.IsNull(options.HeadlessMode);
        Assert.IsNull(options.AutoRunOnStart);
        Assert.IsNull(options.MaxAutoRetries);
        Assert.IsNull(options.MaxApiRetries);
        Assert.IsNull(options.ApiRetryWaitTimeMs);
        Assert.IsNull(options.MultiplyApiRetryWaitTimeByAttempt);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_HeadlessFlag_SetsHeadlessMode()
    {
        // Arrange
        var args = new[] { "--headless" };

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(options.HeadlessMode);
        Assert.IsTrue(options.ShouldRunHeadless);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_AutoRunFlag_SetsAutoRunOnStart()
    {
        // Arrange
        var args = new[] { "--auto-run" };

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(options.AutoRunOnStart);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_FixedRetryWaitFlag_SetsMultiplyToFalse()
    {
        // Arrange
        var args = new[] { "--fixed-retry-wait" };

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.IsFalse(options.MultiplyApiRetryWaitTimeByAttempt);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_MaxRetriesWithValidValue_SetsMaxAutoRetries()
    {
        // Arrange
        var args = new[] { "--max-retries", "10" };

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(10, options.MaxAutoRetries);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_MaxRetriesWithMinValue_Succeeds()
    {
        // Arrange
        var args = new[] { "--max-retries", "1" };

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(1, options.MaxAutoRetries);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_MaxRetriesWithMaxValue_Succeeds()
    {
        // Arrange
        var args = new[] { "--max-retries", "365" };

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(365, options.MaxAutoRetries);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_MaxRetriesBelowMin_ReturnsError()
    {
        // Arrange
        var args = new[] { "--max-retries", "0" };

        // Act
        var result = CommandLineParser.TryParse(args, out _, out var errorMessage);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNotNull(errorMessage);
        Assert.Contains("--max-retries", errorMessage);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_MaxRetriesAboveMax_ReturnsError()
    {
        // Arrange
        var args = new[] { "--max-retries", "366" };

        // Act
        var result = CommandLineParser.TryParse(args, out _, out var errorMessage);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNotNull(errorMessage);
        Assert.Contains("--max-retries", errorMessage);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_MaxRetriesWithoutValue_ReturnsError()
    {
        // Arrange
        var args = new[] { "--max-retries" };

        // Act
        var result = CommandLineParser.TryParse(args, out _, out var errorMessage);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNotNull(errorMessage);
        Assert.Contains("requires a value", errorMessage);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_MaxRetriesWithNonInteger_ReturnsError()
    {
        // Arrange
        var args = new[] { "--max-retries", "abc" };

        // Act
        var result = CommandLineParser.TryParse(args, out _, out var errorMessage);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNotNull(errorMessage);
        Assert.Contains("must be an integer", errorMessage);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_MaxApiRetries_SetsValue()
    {
        // Arrange
        var args = new[] { "--max-api-retries", "5" };

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(5, options.MaxApiRetries);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_MaxApiRetriesZero_Succeeds()
    {
        // Arrange - 0 means no retries
        var args = new[] { "--max-api-retries", "0" };

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(0, options.MaxApiRetries);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_ApiRetryWait_SetsValue()
    {
        // Arrange
        var args = new[] { "--api-retry-wait", "200" };

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(200, options.ApiRetryWaitTimeMs);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_ApiRetryWaitBelowMin_ReturnsError()
    {
        // Arrange
        var args = new[] { "--api-retry-wait", "0" };

        // Act
        var result = CommandLineParser.TryParse(args, out _, out var errorMessage);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNotNull(errorMessage);
        Assert.Contains("--api-retry-wait", errorMessage);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_ApiRetryWaitAboveMax_ReturnsError()
    {
        // Arrange
        var args = new[] { "--api-retry-wait", "1001" };

        // Act
        var result = CommandLineParser.TryParse(args, out _, out var errorMessage);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNotNull(errorMessage);
        Assert.Contains("--api-retry-wait", errorMessage);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_MultipleFlags_AllSet()
    {
        // Arrange
        var args = new[] { "--headless", "--auto-run", "--fixed-retry-wait" };

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(options.HeadlessMode);
        Assert.IsTrue(options.AutoRunOnStart);
        Assert.IsFalse(options.MultiplyApiRetryWaitTimeByAttempt);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_AllOptions_AllSet()
    {
        // Arrange
        var args = new[]
        {
            "--headless",
            "--auto-run",
            "--max-retries", "20",
            "--max-api-retries", "5",
            "--api-retry-wait", "150",
            "--fixed-retry-wait"
        };

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(options.HeadlessMode);
        Assert.IsTrue(options.AutoRunOnStart);
        Assert.AreEqual(20, options.MaxAutoRetries);
        Assert.AreEqual(5, options.MaxApiRetries);
        Assert.AreEqual(150, options.ApiRetryWaitTimeMs);
        Assert.IsFalse(options.MultiplyApiRetryWaitTimeByAttempt);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_HelpFlag_ReturnsHelpText()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var result = CommandLineParser.TryParse(args, out _, out var errorMessage);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNotNull(errorMessage);
        Assert.Contains("HOSUnlock", errorMessage);
        Assert.Contains("Usage", errorMessage);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_ShortHelpFlag_ReturnsHelpText()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act
        var result = CommandLineParser.TryParse(args, out _, out var errorMessage);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNotNull(errorMessage);
        Assert.Contains("HOSUnlock", errorMessage);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_UnknownFlag_ReturnsError()
    {
        // Arrange
        var args = new[] { "--unknown-flag" };

        // Act
        var result = CommandLineParser.TryParse(args, out _, out var errorMessage);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNotNull(errorMessage);
        Assert.Contains("Unknown argument", errorMessage);
        Assert.Contains("--unknown-flag", errorMessage);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void TryParse_CaseInsensitive_Succeeds()
    {
        // Arrange
        var args = new[] { "--HEADLESS", "--AUTO-RUN" };

        // Act
        var result = CommandLineParser.TryParse(args, out var options, out _);

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(options.HeadlessMode);
        Assert.IsTrue(options.AutoRunOnStart);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetHelpText_ContainsAllOptions()
    {
        // Act
        var helpText = CommandLineParser.GetHelpText();

        // Assert
        Assert.Contains("--headless", helpText);
        Assert.Contains("--auto-run", helpText);
        Assert.Contains("--max-retries", helpText);
        Assert.Contains("--max-api-retries", helpText);
        Assert.Contains("--api-retry-wait", helpText);
        Assert.Contains("--fixed-retry-wait", helpText);
        Assert.Contains("--help", helpText);
    }
}
