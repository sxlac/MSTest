using Refit;
using Signify.Spirometry.Core.ApiClients.CdiApi.Holds.Requests;
using System;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.ApiClients.CdiApi.Holds
{
    /// <summary>
    /// Interface to make requests to the CDI Holds API
    /// </summary>
    /// <remarks>
    /// See https://dev.azure.com/signifyhealth/HCC/_git/cdi?path=/legacy/api/cdiapi/src/CH.CDI.WebApi/Controllers
    /// </remarks>
    public interface ICdiHoldsApi
    {
        [Put("/{cdiHoldId}/release")]
        [Headers("Authorization: Bearer")]
        Task<IApiResponse> ReleaseHold(Guid cdiHoldId, [Body] ReleaseHoldRequest request);
    }
}
