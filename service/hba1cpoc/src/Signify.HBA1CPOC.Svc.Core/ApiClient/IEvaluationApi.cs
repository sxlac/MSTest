using Refit;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.ApiClient;

public interface IEvaluationApi
{

	[Get("/evaluationVersion/{evaluationId}")]
	[Headers("Authorization: Bearer")]
	Task<EvaluationVersionRs> GetEvaluationVersion(int evaluationId, string version = "Latest");

	[Get("/evaluationVersion/{evaluationId}")]
	[Headers("Authorization: Bearer")]
	Task<IApiResponse<IList<EvaluationStatusHistory>>> GetEvaluationStatusHistory(long evaluationId);
}