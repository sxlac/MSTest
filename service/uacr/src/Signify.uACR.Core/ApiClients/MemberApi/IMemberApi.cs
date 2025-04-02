using System.Threading.Tasks;
using Refit;
using Signify.uACR.Core.ApiClients.MemberApi.Responses;

namespace Signify.uACR.Core.ApiClients.MemberApi;

/// <summary>
/// Interface to make requests to the Signify Member core API
/// </summary>
public interface IMemberApi
{
    /// <summary>
    /// Returns a member object given a member plan id
    /// </summary>
    /// <param name="memberPlanId">Member Plan Id</param>
    /// <returns><see cref="MemberInfo"/></returns>
    [Get("/member/{memberPlanId}")]
    [Headers("Authorization: Bearer")]
    Task<MemberInfo> GetMemberByMemberPlanId(long memberPlanId);
}