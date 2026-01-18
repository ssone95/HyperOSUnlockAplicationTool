using HOSUnlock.Configuration;
using HOSUnlock.Constants;
using HOSUnlock.Exceptions;
using HOSUnlock.Models;
using HOSUnlock.Models.Base;
using HOSUnlock.Models.Common;

namespace HOSUnlock.Services;

/// <summary>
/// Interface for MiAuthRequestProcessor to enable mocking in tests.
/// </summary>
public interface IMiAuthRequestProcessor : IDisposable
{
    Task<Dictionary<TokenInfo, BaseResponse<BlCheckResponseDto>>> StartAsync();
    Task<BaseResponse<BlCheckResponseDto>> RunSingleBlCheckAsync(TokenInfo tokenInfo);
    Task<BaseResponse<ApplyBlAuthResponseDto>> ApplyForUnlockAsync(TokenShiftDefinition tokenShift);
    Task StopAsync();
}

/// <summary>
/// Factory for creating IMiDataService instances.
/// </summary>
public interface IMiDataServiceFactory
{
    IMiDataService Create(string cookieValue, CancellationToken cancellationToken);
}

/// <summary>
/// Default factory that creates MiDataService instances.
/// </summary>
public sealed class MiDataServiceFactory : IMiDataServiceFactory
{
    public IMiDataService Create(string cookieValue, CancellationToken cancellationToken)
        => new MiDataService(cookieValue, cancellationToken);
}

public sealed class MiAuthRequestProcessor : IMiAuthRequestProcessor
{
    private readonly Dictionary<TokenInfo, IMiDataService> _miDataServices;
    private readonly CancellationTokenSource _ctSource;
    private bool _disposed;

    public MiAuthRequestProcessor(AppConfiguration configuration, CancellationTokenSource ctSource)
        : this(configuration, ctSource, new MiDataServiceFactory())
    {
    }

    public MiAuthRequestProcessor(
        AppConfiguration configuration,
        CancellationTokenSource ctSource,
        IMiDataServiceFactory dataServiceFactory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(ctSource);
        ArgumentNullException.ThrowIfNull(dataServiceFactory);

        _ctSource = ctSource;
        _miDataServices = configuration.Tokens
            .OrderBy(x => x.Index)
            .ToDictionary(
                tokenInfo => tokenInfo,
                tokenInfo => dataServiceFactory.Create(tokenInfo.Token, _ctSource.Token));
    }

    public async Task<Dictionary<TokenInfo, BaseResponse<BlCheckResponseDto>>> StartAsync()
    {
        var preCheckResults = await GetPreCheckStatusesAsync().ConfigureAwait(false);

        if (!preCheckResults.Values.All(x => x.Data is not null))
        {
            Logger.LogError("Pre-check failed. One or more MiDataServices could not retrieve status.");
            throw new InvalidOperationException("Pre-check failed. Cannot start MiAuthRequestProcessor.");
        }

        return preCheckResults;
    }

    public async Task<BaseResponse<BlCheckResponseDto>> RunSingleBlCheckAsync(TokenInfo tokenInfo)
    {
        try
        {
            if (!_miDataServices.TryGetValue(tokenInfo, out var miDataService))
            {
                Logger.LogError($"No MiDataService found with token index {tokenInfo.Index}.");
                throw new MiException("Invalid token index.", MiDataConstants.STATUS_CODE_OTHER_FAILURE);
            }

            var response = await miDataService.GetStatusCheckResultAsync().ConfigureAwait(false);

            if (response?.Data is null)
            {
                Logger.LogError($"MiDataService {tokenInfo.Index} - Failed to retrieve BL check result. Response Code: {response?.Code}, Message: {response?.Message}");
                throw new MiException("Failed to retrieve BL check result.", response?.Code ?? MiDataConstants.STATUS_CODE_OTHER_FAILURE);
            }

            return response;
        }
        catch (MiException ex) when (ex.StatusCode == MiDataConstants.STATUS_COOKIE_EXPIRED_OR_INVALID)
        {
            Logger.LogError($"Cookie expired or invalid for token {tokenInfo.Index}.\nPlease check your token configuration for validity!", ex);
            throw;
        }
        catch (MiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error during BL check for token {tokenInfo.Index}.", ex);
            throw new MiException("Error during BL check.", ex, MiDataConstants.STATUS_CODE_OTHER_FAILURE);
        }
    }

    private async Task<Dictionary<TokenInfo, BaseResponse<BlCheckResponseDto>>> GetPreCheckStatusesAsync()
    {
        var tasks = _miDataServices.Keys
            .Select(async key => (Key: key, Result: await RunSingleBlCheckAsync(key).ConfigureAwait(false)))
            .ToList();

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        var dictionary = new Dictionary<TokenInfo, BaseResponse<BlCheckResponseDto>>();
        foreach (var (key, result) in results)
        {
            if (result?.Data is null)
            {
                Logger.LogError($"MiDataService {key.Index} - Failed to retrieve status. Response Code: {result?.Code}, Message: {result?.Message}");
                throw new MiException("Failed to retrieve status during pre-check.", result?.Code ?? -1);
            }
            dictionary.Add(key, result);
        }

        return dictionary;
    }

    public async Task<BaseResponse<ApplyBlAuthResponseDto>> ApplyForUnlockAsync(TokenShiftDefinition tokenShift)
    {
        try
        {
            var tokenInfo = tokenShift.ToTokenInfo();

            if (!_miDataServices.TryGetValue(tokenInfo, out var miDataService))
            {
                Logger.LogError($"No MiDataService found for token index {tokenInfo.Index}.");
                throw new MiException("Invalid token index.", MiDataConstants.STATUS_CODE_OTHER_FAILURE);
            }

            var response = await miDataService.GetApplyAuthForUnlockResultAsync().ConfigureAwait(false);

            if (response?.Data is null)
            {
                Logger.LogError($"MiDataService {tokenInfo.Index} - Failed to apply for unlock. Response Code: {response?.Code}, Message: {response?.Message}");
                throw new MiException("Failed to apply for unlock.", response?.Code ?? MiDataConstants.STATUS_CODE_OTHER_FAILURE);
            }

            return response;
        }
        catch (MiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error applying for unlock with token {tokenShift.TokenIndex} shift {tokenShift.ShiftIndex}.", ex);
            throw new MiException("Error applying for unlock.", ex, MiDataConstants.STATUS_CODE_OTHER_FAILURE);
        }
    }

    public async Task StopAsync()
    {
        try
        {
            await _ctSource.CancelAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error stopping MiAuthRequestProcessor.", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var service in _miDataServices.Values)
        {
            service.Dispose();
        }

        _miDataServices.Clear();
    }
}
