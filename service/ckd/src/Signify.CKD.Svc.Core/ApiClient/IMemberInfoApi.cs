using System.Threading.Tasks;
using Refit;
using Signify.CKD.Svc.Core.ApiClient.Response;

namespace Signify.CKD.Svc.Core.ApiClient
{
    public interface IMemberInfoApi
	{

		[Get("/member/{id}")]
		[Headers("Authorization: Bearer")]
		Task<ApiResponse<MemberInfoRs>> GetMemberInfoById(int id);
	}
}
