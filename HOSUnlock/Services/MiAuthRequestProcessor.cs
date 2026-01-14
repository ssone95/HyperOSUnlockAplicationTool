using HOSUnlock.Configuration;
using HOSUnlock.Constants;
using HOSUnlock.Exceptions;
using HOSUnlock.Models;
using HOSUnlock.Models.Base;
using HOSUnlock.Models.Common;
using static HOSUnlock.Configuration.AppConfiguration;

namespace HOSUnlock.Services
{
    public class MiAuthRequestProcessor : IDisposable
    {
        private readonly Dictionary<TokenInfo, MiDataService> _miDataServices;
        private readonly CancellationTokenSource _ctSource;
        public MiAuthRequestProcessor(AppConfiguration configuration, CancellationTokenSource ctSource)
        {
            _ctSource = ctSource;
            _miDataServices = configuration.Tokens.Select(x => (Service: new MiDataService(x.Token, _ctSource.Token), TokenInfo: x))
                .OrderBy(x => x.TokenInfo.Index)
                .ToDictionary(x => x.TokenInfo, x => x.Service);
        }

        public async Task<Dictionary<TokenInfo, BaseResponse<BlCheckResponseDto>>> Start()
        {
            var preCheckResults = await GetPreCheckStatuses();
            if (!preCheckResults.All(x => x.Value.Data is not null))
            {
                Logger.LogError("Pre-check failed. One or more MiDataServices could not retrieve status.");
                throw new InvalidOperationException("Pre-check failed. Cannot start MiAuthRequestProcessor.");
            }
            return preCheckResults;
        }

        public async Task<BaseResponse<BlCheckResponseDto>> RunSingleBlCheck(TokenInfo tokenInfo)
        {
            try
            {
                var miDataService = _miDataServices.GetValueOrDefault(tokenInfo);
                if (miDataService == null)
                {
                    Logger.LogError($"No MiDataService found with token index {tokenInfo.Index}.");
                    throw new MiException("Invalid token index.", statusCode: MiDataConstants.STATUS_CODE_OTHER_FAILURE);
                }
                var response = await miDataService.GetStatusCheckResult();
                if (response == null || response.Data == null)
                {
                    Logger.LogError($"MiDataService {tokenInfo.Index} - Failed to retrieve BL check result. Response Code: {response?.Code}, Message: {response?.Message}");
                    throw new MiException("Failed to retrieve BL check result.", response?.Code ?? MiDataConstants.STATUS_CODE_OTHER_FAILURE);
                }
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during BL check for token {tokenInfo.Index}.", ex);
                throw new MiException("Error during BL check.", ex, statusCode: MiDataConstants.STATUS_CODE_OTHER_FAILURE);
            }
        }

        private async Task<Dictionary<TokenInfo, BaseResponse<BlCheckResponseDto>>> GetPreCheckStatuses()
        {
            var serviceTasks = _miDataServices.Keys.Select(x => (Key: x, DataTask: RunSingleBlCheck(x))).ToList();
            await Task.WhenAll(serviceTasks.Select(x => x.DataTask));

            Dictionary<TokenInfo, BaseResponse<BlCheckResponseDto>> results = [];
            foreach (var response in serviceTasks)
            {
                var key = response.Key;
                var dataResponse = response.DataTask.Result;
                if (dataResponse == null || dataResponse.Data == null)
                {
                    Logger.LogError($"MiDataService {key} - Failed to retrieve status. Response Code: {dataResponse?.Code}, Message: {dataResponse?.Message}");
                    throw new MiException("Failed to retrieve status during pre-check.", dataResponse?.Code ?? -1);
                }
                results.Add(key, dataResponse);
            }

            return results;
        }

        public async Task<BaseResponse<ApplyBlAuthResponseDto>> ApplyForUnlock(TokenShiftDefinition tokenShift)
        {
            try
            {
                var tokenInfo = tokenShift.GetTokenInfo();
                var miDataService = _miDataServices.GetValueOrDefault(tokenInfo);
                if (miDataService == null)
                {
                    Logger.LogError($"No MiDataService found for token index {tokenInfo.Index}.");
                    throw new MiException("Invalid token index.", statusCode: MiDataConstants.STATUS_CODE_OTHER_FAILURE);
                }

                var response = await miDataService.GetApplyAuthForUnlockResult();
                if (response == null || response.Data == null)
                {
                    Logger.LogError($"MiDataService {tokenInfo.Index} - Failed to apply for unlock. Response Code: {response?.Code}, Message: {response?.Message}");
                    throw new MiException("Failed to apply for unlock.", response?.Code ?? MiDataConstants.STATUS_CODE_OTHER_FAILURE);
                }

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error applying for unlock with token {tokenShift.TokenIndex} shift {tokenShift.ShiftIndex}.", ex);
                throw new MiException("Error applying for unlock.", ex, statusCode: MiDataConstants.STATUS_CODE_OTHER_FAILURE);
            }
        }

        public async Task Stop()
        {
            try
            {
                await _ctSource.CancelAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error stopping MiAuthRequestProcessor.", ex);
            }
        }

        public void Dispose()
        {
            foreach (var service in _miDataServices.Values)
            {
                service.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}
