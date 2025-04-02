using System.Threading.Tasks;
using Refit;
using Signify.A1C.Svc.Core.ApiClient.Response;

namespace Signify.A1C.Svc.Core.ApiClient
{
    public interface IMemberInfoApi
    {

        [Get("/member/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<MemberInfoRs>> GetMemberInfoById(int id);
    }
}