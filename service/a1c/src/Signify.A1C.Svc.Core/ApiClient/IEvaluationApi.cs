using Refit;
using Signify.A1C.Svc.Core.ApiClient.Response;
using System.Threading.Tasks;

namespace Signify.A1C.Svc.Core.ApiClient
{
	public interface IEvaluationApi
	{

		[Get("/evaluationVersion/{id}")]
		[Headers("Authorization: Bearer")]
		Task<EvaluationVersionRs> GetEvaluationVersion(int id, string version = "Latest");
	}
}
