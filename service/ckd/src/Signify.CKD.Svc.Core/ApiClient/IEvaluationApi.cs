using System.Threading.Tasks;
using Refit;
using Signify.CKD.Svc.Core.ApiClient.Response;

namespace Signify.CKD.Svc.Core.ApiClient
{
	/// <summary>
	/// Interface to make requests to the Signify Evaluation core API
	///
	/// See https://dev.azure.com/signifyhealth/HCC/_git/coreservices?path=/api/signify.evaluationapi.webapi/src/Signify.EvaluationsApi.WebApi/Controllers/EvaluationVersionController.cs
	/// </summary>
	public interface IEvaluationApi
	{
		[Get("/evaluationVersion/{id}")]
		[Headers("Authorization: Bearer")]
		Task<EvaluationVersionRs> GetEvaluationVersion(int id, string version = "Latest");
	}
}
