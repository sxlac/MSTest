using Refit;
using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.ApiClients.EvaluationApi
{
    /// <summary>
    /// Interface to make requests to the Signify Evaluation core API
    ///
    /// See https://chgit.censeohealth.com/projects/EV/repos/evaluationsapi/browse/src/CH.Evaluations.WebApi/Controllers
    /// </summary>
    public interface IEvaluationApi
    {
        /// <summary>
        /// Get Evaluation Version
        /// </summary>
        /// <param name="id">Evaluation Id</param>
        /// <param name="version"></param>
        /// <returns></returns>
        [Get("/evaluationVersion/{id}")]
        [Headers("Authorization: Bearer")]
        Task<EvaluationVersion> GetEvaluationVersion(int id, string version = "Latest");

        /// <summary>
        /// Gets a collection of evaluations associated with the given <see cref="memberPlanId"/>
        /// </summary>
        /// <param name="memberPlanId"></param>
        /// <returns></returns>
        [Get("/memberPlan/{memberPlanId}")]
        [Headers("Authorization: Bearer")]
        Task<IApiResponse<ICollection<Evaluation>>> GetEvaluations(long memberPlanId);
    }
}
