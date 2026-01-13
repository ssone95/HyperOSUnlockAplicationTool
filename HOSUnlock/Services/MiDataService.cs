using HOSUnlock.Constants;
using HOSUnlock.Exceptions;
using HOSUnlock.Models;
using HOSUnlock.Models.Base;
using System.Text;
using System.Text.Json;

namespace HOSUnlock.Services
{
    public class MiDataService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly CancellationToken ct;
        public MiDataService(string cookieValue, CancellationToken ct)
        {
            this.ct = ct;

            var _deviceId = MiDataConstants.GetRandomDeviceId();

            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Add(
                MiDataConstants.CookieHeaderKey,
                MiDataConstants.GetCookieValue(cookieValue, _deviceId));
        }

        public async Task<BaseResponse<BlCheckResponseDto>> GetStatusCheckResult()
        {
            return await SendRequest<object, BlCheckResponseDto>(
                HttpMethod.Get,
                MiDataConstants.MI_DATA_BL_SWITCH_CHECK,
                null,
                ct);
        }

        public async Task<BaseResponse<ApplyBlAuthResponseDto>> GetApplyAuthForUnlockResult()
        {
            return await SendRequest<object, ApplyBlAuthResponseDto>(
                HttpMethod.Post,
                MiDataConstants.MI_DATA_APLY_AUTH,
                null,
                ct);
        }

        private async Task<BaseResponse<TOut>> SendRequest<TIn, TOut>(HttpMethod method, string endpoint, TIn? body = null, CancellationToken ct = default)
            where TIn : class
            where TOut : class
        {
            try
            {
                using var requestMessage = new HttpRequestMessage(method, MiDataConstants.FormatFullUrl(endpoint));

                if (method == HttpMethod.Post)
                {
                    requestMessage.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                    requestMessage.Headers.Add("User-Agent", "okhttp/4.12.0");
                    requestMessage.Headers.Add("Connection", "keep-alive");

                    byte[] bodyData = body is null
                        ? Encoding.UTF8.GetBytes("{\"is_retry\":true}")
                        : Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body));

                    requestMessage.Content = new ByteArrayContent(bodyData);
                }
                // Accepts header
                requestMessage.Headers.Add("Accept", "application/json");

                using var response = await _httpClient.SendAsync(requestMessage, ct);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(ct);
                var apiResponse = JsonSerializer.Deserialize<BaseResponse<TOut>>(responseContent);

                return apiResponse
                    ?? throw new MiException("Failed to deserialize API response.", statusCode: MiDataConstants.STATUS_CODE_OTHER_FAILURE);
            }
            catch (OperationCanceledException cancelEx) when (ct.IsCancellationRequested)
            {
                throw new MiException("The API request was canceled.", cancelEx, MiDataConstants.STATUS_CODE_OTHER_FAILURE);
            }
            catch (JsonException jsonEx)
            {
                throw new MiException("Failed to parse API response JSON.", jsonEx, MiDataConstants.STATUS_CODE_OTHER_FAILURE);
            }
            catch (Exception ex)
            {
                throw new MiException("An error occurred while sending API request.", ex, MiDataConstants.STATUS_CODE_OTHER_FAILURE);
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
