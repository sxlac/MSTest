using System.Threading.Tasks;
using Refit;
using Signify.PAD.Svc.Core.ApiClient.Response;

namespace Signify.PAD.Svc.Core.ApiClient;

public interface IMemberInfoApi
{
    /// <summary>
    /// Returns a member object given a member plan id
    /// </summary>
    /// <param name="memberPlanId"></param>
    /// <returns><see cref="MemberInfoRs"/></returns>
    /// <remarks>https://developer.signifyhealth.com/catalog/default/api/member/definition#/Member/get_member__id_</remarks>
    [Get("/member/{memberPlanId}")]
    [Headers("Authorization: Bearer")]
    Task<MemberInfoRs> GetMemberByMemberPlanId(long memberPlanId);
}