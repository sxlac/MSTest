using System.Threading.Tasks;
using Refit;
using Signify.FOBT.Svc.Core.ApiClient.Response;

namespace Signify.FOBT.Svc.Core.ApiClient;

/// <summary>
/// Interface to make requests to the Signify Member core API
/// </summary>
public interface IMemberInfoApi
{
    /// <summary>
    /// Returns a member object given a member plan id
    /// </summary>
    /// <param name="memberPlanId">Member Plan Id</param>
    /// <returns><see cref="MemberInfoRs"/></returns>
    [Get("/member/{memberPlanId}")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<MemberInfoRs>> GetMemberInfoById(int memberPlanId);
}