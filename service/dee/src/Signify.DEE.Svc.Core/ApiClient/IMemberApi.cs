using System.Threading.Tasks;
using Signify.DEE.Svc.Core.Messages.Models;
using Refit;

namespace Signify.DEE.Svc.Core.ApiClient;

public interface IMemberApi
{
    [Get("/member/{id}")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<MemberModel>> GetMember([AliasAs("Id")] long memberPlanId);
}