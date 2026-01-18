using HOSUnlock.Exceptions;

namespace HOSUnlocker.Tests.Exceptions;

[TestClass]
public sealed class MiExceptionTests
{
    private const int TestTimeoutMs = 360000; // 6 minutes

    #region Constructor Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constructor_Default_SetsDefaultStatusCode()
    {
        // Arrange & Act
        var exception = new MiException();

        // Assert
        Assert.AreEqual(-1, exception.StatusCode);
        Assert.IsNull(exception.InnerException);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new MiException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(-1, exception.StatusCode);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constructor_WithMessageAndStatusCode_SetsBoth()
    {
        // Arrange
        var message = "Test error message";
        var statusCode = 100001;

        // Act
        var exception = new MiException(message, statusCode);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(statusCode, exception.StatusCode);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constructor_WithInnerException_SetsInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var message = "Outer error";

        // Act
        var exception = new MiException(message, innerException);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
        Assert.AreEqual(-1, exception.StatusCode);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constructor_WithInnerExceptionAndStatusCode_SetsAll()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var message = "Outer error";
        var statusCode = 100004;

        // Act
        var exception = new MiException(message, innerException, statusCode);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
        Assert.AreEqual(statusCode, exception.StatusCode);
    }

    #endregion
}
