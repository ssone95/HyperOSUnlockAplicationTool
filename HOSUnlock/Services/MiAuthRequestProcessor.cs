using HOSUnlock.Configuration;
using HOSUnlock.Constants;
using HOSUnlock.Exceptions;
using HOSUnlock.Models;
using HOSUnlock.Models.Base;

namespace HOSUnlock.Services
{
    public class MiAuthRequestProcessor : IDisposable
    {
        private readonly Dictionary<int, MiDataService> _miDataServices;
        private readonly CancellationTokenSource _ctSource;
        public MiAuthRequestProcessor(AppConfiguration configuration, CancellationTokenSource ctSource)
        {
            _ctSource = ctSource;
            _miDataServices = new Dictionary<int, MiDataService>()
            {
                [1] = new MiDataService(configuration.Token1, _ctSource.Token),
                [2] = new MiDataService(configuration.Token2, _ctSource.Token),
                [3] = new MiDataService(configuration.Token3, _ctSource.Token),
                [4] = new MiDataService(configuration.Token4, _ctSource.Token)
            };
        }

        public async Task<Dictionary<int, BaseResponse<BlCheckResponseDto>>> Start()
        {
            var preCheckResults = await GetPreCheckStatuses();
            if (!preCheckResults.All(x => x.Value.Data is not null))
            {
                Logger.LogError("Pre-check failed. One or more MiDataServices could not retrieve status.");
                throw new InvalidOperationException("Pre-check failed. Cannot start MiAuthRequestProcessor.");
            }
            return preCheckResults;
        }

        public async Task<BaseResponse<BlCheckResponseDto>> RunSingleBlCheck(int index)
        {
            try
            {
                var miDataService = _miDataServices.GetValueOrDefault(index);
                if (miDataService == null)
                {
                    Logger.LogError($"No MiDataService found for index {index}.");
                    throw new MiException("Invalid index.", statusCode: MiDataConstants.STATUS_CODE_OTHER_FAILURE);
                }
                var response = await miDataService.GetStatusCheckResult();
                if (response == null || response.Data == null)
                {
                    Logger.LogError($"MiDataService {index} - Failed to retrieve BL check result. Response Code: {response?.Code}, Message: {response?.Message}");
                    throw new MiException("Failed to retrieve BL check result.", response?.Code ?? MiDataConstants.STATUS_CODE_OTHER_FAILURE);
                }
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during BL check for index {index}.", ex);
                throw new MiException("Error during BL check.", ex, statusCode: MiDataConstants.STATUS_CODE_OTHER_FAILURE);
            }
        }

        private async Task<Dictionary<int, BaseResponse<BlCheckResponseDto>>> GetPreCheckStatuses()
        {
            var serviceTasks = _miDataServices.Keys.Select(x => (Key: x, DataTask: RunSingleBlCheck(x))).ToList();
            await Task.WhenAll(serviceTasks.Select(x => x.DataTask));

            Dictionary<int, BaseResponse<BlCheckResponseDto>> results = [];
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

        public async Task<BaseResponse<ApplyBlAuthResponseDto>> ApplyForUnlock(int tokenIndex)
        {
            try
            {
                var miDataService = _miDataServices.GetValueOrDefault(tokenIndex);
                if (miDataService == null)
                {
                    Logger.LogError($"No MiDataService found for token index {tokenIndex}.");
                    throw new MiException("Invalid token index.", statusCode: MiDataConstants.STATUS_CODE_OTHER_FAILURE);
                }

                var response = await miDataService.GetApplyAuthForUnlockResult();
                if (response == null || response.Data == null)
                {
                    Logger.LogError($"MiDataService {tokenIndex} - Failed to apply for unlock. Response Code: {response?.Code}, Message: {response?.Message}");
                    throw new MiException("Failed to apply for unlock.", response?.Code ?? MiDataConstants.STATUS_CODE_OTHER_FAILURE);
                }

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error applying for unlock with token index {tokenIndex}.", ex);
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
