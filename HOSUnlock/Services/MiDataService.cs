using HOSUnlock.Constants;
using HOSUnlock.Exceptions;
using HOSUnlock.Models;
using HOSUnlock.Models.Base;
using Polly;
using System.Text;
using System.Text.Json;

namespace HOSUnlock.Services;

public sealed class MiDataService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly CancellationToken _cancellationToken;
    private readonly ResiliencePipeline _apiRetryPipeline;
    private bool _disposed;

    public MiDataService(string cookieValue, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cookieValue);

        _cancellationToken = cancellationToken;

        var deviceId = MiDataConstants.GetRandomDeviceId();

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Add(
            MiDataConstants.CookieHeaderKey,
            MiDataConstants.GetCookieValue(cookieValue, deviceId));

        _apiRetryPipeline = ResiliencePolicies.CreateAsyncRetryPipeline("Mi API Request");
    }

    public Task<BaseResponse<BlCheckResponseDto>> GetStatusCheckResultAsync()
        => SendRequestAsync<BlCheckResponseDto>(
            HttpMethod.Get,
            MiDataConstants.MI_DATA_BL_SWITCH_CHECK);

    public Task<BaseResponse<ApplyBlAuthResponseDto>> GetApplyAuthForUnlockResultAsync()
        => SendRequestAsync<ApplyBlAuthResponseDto>(
            HttpMethod.Post,
            MiDataConstants.MI_DATA_APLY_AUTH,
            """{"is_retry":true}""");

    private async Task<BaseResponse<TOut>> SendRequestAsync<TOut>(
        HttpMethod method,
        string endpoint,
        string? jsonBody = null)
        where TOut : class
    {
        try
        {
            return await _apiRetryPipeline.ExecuteAsync(async cancellationToken =>
            {
                using var requestMessage = new HttpRequestMessage(method, MiDataConstants.FormatFullUrl(endpoint));

                requestMessage.Headers.Add("Accept", "application/json");

                if (method == HttpMethod.Post)
                {
                    requestMessage.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                    requestMessage.Headers.Add("User-Agent", "okhttp/4.12.0");
                    requestMessage.Headers.Add("Connection", "keep-alive");

                    var bodyContent = jsonBody ?? "{}";
                    requestMessage.Content = new StringContent(bodyContent, Encoding.UTF8, "application/json");
                }

                using var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                return JsonSerializer.Deserialize<BaseResponse<TOut>>(responseContent, JsonOptions)
                    ?? throw new MiException("Failed to deserialize API response.", MiDataConstants.STATUS_CODE_OTHER_FAILURE);
            }, _cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
        {
            throw new MiException("The API request was canceled.", MiDataConstants.STATUS_CODE_OTHER_FAILURE);
        }
        catch (JsonException ex)
        {
            throw new MiException("Failed to parse API response JSON.", ex, MiDataConstants.STATUS_CODE_OTHER_FAILURE);
        }
        catch (MiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new MiException("An error occurred while sending API request.", ex, MiDataConstants.STATUS_CODE_OTHER_FAILURE);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _httpClient.Dispose();
    }
}
