using Refit;
using Signify.PAD.Svc.Core.ApiClient.Requests;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.Models;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.ApiClient;

public interface IEvaluationApi
{
    /// <summary>
    /// Represents a revision (or initial version) of an evaluation including the Question and Answer details 
    /// </summary>
    /// <param name="evaluationId">The evaluation identifier.</param>
    /// <param name="version">The version to retrieve. Acceptable values are "latest", or a numerical value.</param>
    /// <returns><see cref="EvaluationVersionRs"/></returns>
    /// <remarks>https://developer.signifyhealth.com/catalog/default/api/evaluation/definition#/EvaluationVersion/get_evaluationversion__id_</remarks>
    [Get("/evaluationVersion/{evaluationId}")]
    [Headers("Authorization: Bearer")]
    Task<EvaluationVersionRs> GetEvaluationVersion(long evaluationId, string version = "Latest");

    /// <summary>
    /// Retrieves a list of <see cref="EvaluationStatusHistory"/> containing the status of each version of the evaluation.
    /// Input parameter `version` parameter is set to `null` and `expand` is set to `default/false`
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
    /// <remarks>https://developer.signifyhealth.com/catalog/default/api/evaluation/definition#/EvaluationVersion/get_evaluationversion__id_</remarks>
    [Get("/evaluationVersion/{evaluationId}")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<IList<EvaluationStatusHistory>>> GetEvaluationStatusHistory(long evaluationId);

    /// <summary>
    /// Uploads a document based on EvaluationId
    /// </summary>
    /// <param name="evaluationId">The evaluation identifier.</param>
    /// <param name="byteArrayPart"></param>
    /// <param name="evaluationRequest"></param>
    /// <returns></returns>
    /// <remarks>https://developer.signifyhealth.com/catalog/default/api/evaluation/definition#/EvaluationDocument/post_evaluationdocument__id_</remarks>
    [Multipart]
    [Post("/evaluationDocument/{id}")]
    [Headers("Authorization: Bearer", "Accept:" + MediaTypeNames.Application.Pdf)]
    Task<IApiResponse<EvaluationDocumentModel>> CreateEvaluationDocument([AliasAs("Id")] int evaluationId, [AliasAs("Form")] ByteArrayPart byteArrayPart,
        [Query] EvaluationRequest evaluationRequest);

    /// <summary>
    /// Returns document(s) based on EvaluationId
    /// </summary>
    /// <param name="evaluationId">The evaluation identifier.</param>
    /// <param name="documentType"></param>
    /// <returns></returns>
    /// <remarks>https://developer.signifyhealth.com/catalog/default/api/evaluation/definition#/EvaluationDocument/get_evaluationdocument__id_</remarks>
    [Get("/evaluationDocument/{id}")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<IList<EvaluationDocumentModel>>> GetEvaluationDocumentDetails([AliasAs("Id")] int evaluationId, string documentType);
}