using HOSUnlock.Constants;

namespace HOSUnlocker.Tests.Constants;

[TestClass]
public sealed class MiDataConstantsTests
{
    private const int TestTimeoutMs = 360000; // 6 minutes

    #region FormatFullUrl Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void FormatFullUrl_BlSwitchCheck_ReturnsCorrectUrl()
    {
        // Act
        var url = MiDataConstants.FormatFullUrl(MiDataConstants.MI_DATA_BL_SWITCH_CHECK);

        // Assert
        Assert.StartsWith("https://", url);
        Assert.Contains("sgp-api.buy.mi.com", url);
        Assert.Contains(MiDataConstants.MI_DATA_BL_SWITCH_CHECK, url);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void FormatFullUrl_ApplyAuth_ReturnsCorrectUrl()
    {
        // Act
        var url = MiDataConstants.FormatFullUrl(MiDataConstants.MI_DATA_APLY_AUTH);

        // Assert
        Assert.StartsWith("https://", url);
        Assert.Contains("sgp-api.buy.mi.com", url);
        Assert.Contains(MiDataConstants.MI_DATA_APLY_AUTH, url);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void FormatFullUrl_CustomEndpoint_FormatsCorrectly()
    {
        // Arrange
        var endpoint = "custom/test/endpoint";

        // Act
        var url = MiDataConstants.FormatFullUrl(endpoint);

        // Assert
        Assert.StartsWith("https://", url);
        Assert.EndsWith(endpoint, url);
    }

    #endregion

    #region GetCookieValue Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetCookieValue_ValidInputs_ContainsAllParts()
    {
        // Arrange
        var cookieValue = "testCookieValue123";
        var deviceId = "ABC123DEF456";

        // Act
        var result = MiDataConstants.GetCookieValue(cookieValue, deviceId);

        // Assert
        Assert.Contains($"new_bbs_serviceToken={cookieValue}", result);
        Assert.Contains($"deviceId={deviceId}", result);
        Assert.Contains("versionCode=500411", result);
        Assert.Contains("versionName=5.4.11", result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetCookieValue_DifferentCookies_ProducesDifferentResults()
    {
        // Arrange
        var cookie1 = "cookie1";
        var cookie2 = "cookie2";
        var deviceId = "device123";

        // Act
        var result1 = MiDataConstants.GetCookieValue(cookie1, deviceId);
        var result2 = MiDataConstants.GetCookieValue(cookie2, deviceId);

        // Assert
        Assert.AreNotEqual(result1, result2);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetCookieValue_DifferentDeviceIds_ProducesDifferentResults()
    {
        // Arrange
        var cookie = "testCookie";
        var deviceId1 = "device1";
        var deviceId2 = "device2";

        // Act
        var result1 = MiDataConstants.GetCookieValue(cookie, deviceId1);
        var result2 = MiDataConstants.GetCookieValue(cookie, deviceId2);

        // Assert
        Assert.AreNotEqual(result1, result2);
    }

    #endregion

    #region GetRandomDeviceId Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetRandomDeviceId_ReturnsNonEmptyString()
    {
        // Act
        var deviceId = MiDataConstants.GetRandomDeviceId();

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(deviceId));
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetRandomDeviceId_ReturnsHexString()
    {
        // Act
        var deviceId = MiDataConstants.GetRandomDeviceId();

        // Assert - SHA1 produces 40 hex characters
        Assert.AreEqual(40, deviceId.Length);
        Assert.IsTrue(deviceId.All(c => char.IsLetterOrDigit(c)));
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetRandomDeviceId_MultipleCalls_ProduceDifferentIds()
    {
        // Act
        var ids = new HashSet<string>();
        for (int i = 0; i < 10; i++)
        {
            ids.Add(MiDataConstants.GetRandomDeviceId());
        }

        // Assert - should have 10 unique IDs (very unlikely to have collisions)
        Assert.HasCount(10, ids);
    }

    #endregion
}
