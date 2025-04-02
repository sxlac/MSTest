using System.Collections.Generic;
using Refit;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.ApiClient;

/// <summary>
/// Interface to make requests to the Signify Evaluation core API
///
/// See https://dev.azure.com/signifyhealth/HCC/_git/coreservices?path=/api/signify.evaluationapi.webapi/src/Signify.EvaluationsApi.WebApi/Controllers/EvaluationVersionController.cs
/// </summary>
public interface IEvaluationApi
{
    /// <summary>
    /// Represents a revision (or initial version) of an evaluation including the Question and Answer details 
    /// </summary>
    /// <param name="evaluationId">Evaluation Id</param>
    /// <param name="version">The version to retrieve. Acceptable values are "latest", or a numerical value.</param>
    /// <returns><see cref="EvaluationVersionRs"/></returns>
    /// <remarks>https://developer.signifyhealth.com/catalog/default/api/evaluation/definition#/EvaluationVersion/get_evaluationversion__id_</remarks>
    [Get("/evaluationVersion/{evaluationId}")]
    [Headers("Authorization: Bearer")]
    Task<EvaluationVersionRs> GetEvaluationVersion(long evaluationId, string version = "Latest");
    
    /// <summary>
    /// Retrieves a list of <see cref="EvaluationStatusHistory"/> containing the status of each version of the evaluation.
    /// Input parameter <b>version</b> parameter is set to <b>null</b> and <b>expand</b> is set to <b>default/false</b>
    /// resulting in just the version details rather than the whole evaluation details.
    /// </summary>
    /// <param name="evaluationId">The evaluation identifier</param>
    /// <returns>List of <see cref="EvaluationStatusHistory"/></returns>
    /// <example>
    /// [
    ///     {
    ///         "version": 2,
    ///         "createdDateTime": "2023-11-13T08:39:56.4870000",
    ///         "evaluationId": 288820,
    ///         "evaluationStatusCodeId": 3,
    ///         "evaluationStatusCode": {
    ///             "id": 3,
    ///             "name": "Finalize"
    ///         }
    ///     },
    ///     {
    ///         "version": 1,
    ///         "createdDateTime": "2023-11-13T08:27:17.1570000",
    ///         "evaluationId": 288820,
    ///         "evaluationStatusCodeId": 4,
    ///         "evaluationStatusCode": {
    ///             "id": 4,
    ///             "name": "Cancel"
    ///         }
    ///     }
    /// ]
    /// </example>
    /// <remarks>Backstage- https://developer.signifyhealth.com/catalog/default/api/evaluation/definition#/EvaluationVersion/get_evaluationversion__id_</remarks>
    [Get("/evaluationVersion/{evaluationId}")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<IList<EvaluationStatusHistory>>> GetEvaluationStatusHistory(long evaluationId);
}