using Refit;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.ApiClient;

public interface IMemberInfoApi
{

	[Get("/member/{id}")]
	[Headers("Authorization: Bearer")]
	Task<IApiResponse<MemberInfoRs>> GetMemberInfoById(int id);
}