using System.Threading.Tasks;
using Refit;
using Signify.Spirometry.Core.ApiClients.MemberApi.Responses;

namespace Signify.Spirometry.Core.ApiClients.MemberApi
{
    /// <summary>
    /// Interface to make requests to the Signify Member core API
    /// </summary>
    public interface IMemberApi
    {
        /// <summary>
        /// Get Member
        /// </summary>
        /// <param name="memberPlanId">Member Plan Id</param>
        /// <returns></returns>
        [Get("/member/{memberPlanId}")]
        [Headers("Authorization: Bearer")]
        Task<MemberInfo> GetMemberByMemberPlanId(long memberPlanId);
    }
}