using System.Security.Cryptography;
using System.Text;

namespace HOSUnlock.Constants;

public static class MiDataConstants
{
    #region Api Endpoints

    private const string ApiBaseUrl = "sgp-api.buy.mi.com/bbs/api/global/";

    public const string MI_DATA_APLY_AUTH = "apply/bl-auth";
    public const string MI_DATA_BL_SWITCH_CHECK = "user/bl-switch/state";

    public static string FormatFullUrl(string endpoint)
        => $"https://{ApiBaseUrl}{endpoint}";

    #endregion

    #region MiDataServiceConstants

    public const string CookieHeaderKey = "Cookie";

    public static string GetCookieValue(string cookieValue, string deviceId)
        => $"new_bbs_serviceToken={cookieValue};versionCode=500411;versionName=5.4.11;deviceId={deviceId};";

    public static string GetRandomDeviceId()
    {
        var randomData = $"{Random.Shared.NextDouble()}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(randomData));
        return Convert.ToHexString(bytes);
    }

    #endregion

    #region Status Codes

    public const int STATUS_CODE_OTHER_FAILURE = -1;
    public const int STATUS_CODE_SUCCESS = 0;
    public const int STATUS_REQUEST_REJECTED = 100001;
    public const int STATUS_REQUEST_POTENTIALLY_VALID = 100003;
    public const int STATUS_COOKIE_EXPIRED = 100004;

    #endregion
}
