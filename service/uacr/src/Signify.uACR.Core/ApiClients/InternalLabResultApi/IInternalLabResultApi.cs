using System.Threading.Tasks;
using Refit;
using Signify.uACR.Core.ApiClients.InternalLabResultApi.Responses;

namespace Signify.uACR.Core.ApiClients.InternalLabResultApi;

/// <summary>
/// Interface to make requests to the Signify Internal Lab Result Api
/// </summary>
public interface IInternalLabResultApi
{
    /// <summary>
    /// Get Lab Result by labResultId
    /// </summary>
    /// <param name="labResultId"></param>
    /// <returns></returns>
    [Get("/labresult/{labResultId}")]
    Task<ApiResponse<GetResultResponse>> GetLabResultByLabResultId(string labResultId);
}