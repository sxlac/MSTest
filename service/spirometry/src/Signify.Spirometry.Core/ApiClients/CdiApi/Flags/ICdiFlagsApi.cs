using Refit;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Requests;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Responses;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.ApiClients.CdiApi.Flags
{
    /// <summary>
    /// Interface to make requests to the CDI System Flags API
    /// </summary>
    /// <remarks>
    /// See https://dev.azure.com/signifyhealth/HCC/_git/cdi?path=/legacy/api/cdiapi/src/CH.CDI.WebApi/Controllers
    /// </remarks>
    public interface ICdiFlagsApi
    {
        [Post("/systemflag")]
        [Headers("Authorization: Bearer")]
        Task<IApiResponse<SaveSystemFlagResponse>> CreateFlag([Body] SaveSystemFlagRequest request);
    }
}
