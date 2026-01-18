using HOSUnlock.Configuration;
using HOSUnlock.Models.Common;
using HOSUnlock.Services;

namespace HOSUnlock.Tests.Services;

[TestClass]
public sealed class ClockProviderTests
{
    private const int TestTimeoutMs = 360000; // 6 minutes

    #region ClockThresholdExceededArgs Tests

    [TestMethod]
    [Timeout(TestTimeoutMs)]
    public void ClockThresholdExceededArgs_Constructor_SetsProperties()
    {
        // Arrange
        var tokenShift = new TokenShiftDefinition(1, "Token%2B", 2, 100);
        var utcTime = DateTime.UtcNow;
        var beijingTime = utcTime.AddHours(8);

        // Act
        var args = new ClockProvider.ClockThresholdExceededArgs(tokenShift, utcTime, beijingTime);

        // Assert
        Assert.AreEqual(tokenShift, args.TokenShiftDetails);
        Assert.AreEqual(utcTime, args.UtcTime);
        Assert.AreEqual(beijingTime, args.BeijingTime);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs)]
    public void ClockThresholdExceededArgs_LocalTime_ConvertsFromUtc()
    {
        // Arrange
        var tokenShift = new TokenShiftDefinition(1, "Token%2B", 1, 50);
        var utcTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var beijingTime = utcTime.AddHours(8);

        // Act
        var args = new ClockProvider.ClockThresholdExceededArgs(tokenShift, utcTime, beijingTime);
        var localTime = args.LocalTime;

        // Assert
        Assert.IsNotNull(localTime);
        // LocalTime should be the UTC time converted to local timezone
        var expectedLocalTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local);
        Assert.AreEqual(expectedLocalTime, localTime);
    }

    #endregion
}
