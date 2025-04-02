using Refit;
using Signify.DEE.Svc.Core.ApiClient.Responses;
using Signify.DEE.Svc.Core.Messages.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.ApiClient;

/// <summary>
/// Interface to make requests to the Signify Evaluation core API
///
/// See https://dev.azure.com/signifyhealth/HCC/_git/coreservices?path=/api/signify.evaluationapi.webapi/src/Signify.EvaluationsApi.WebApi/Controllers
/// </summary>
public interface IEvaluationApi
{
    /// <summary>
    /// Get Evaluation Version
    /// </summary>
    [Get("/evaluationVersion/{id}?version=Latest")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<EvaluationVersion>> GetEvaluationVersion([AliasAs("Id")] long evaluationId);

    [Get("/evaluationVersion/{evaluationId}")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<IList<EvaluationStatusHistory>>> GetEvaluationStatusHistory(long evaluationId);


    [Get("/evaluationDocument/{id}")]
    [Headers("Authorization: Bearer")]
    Task<IApiResponse<List<EvaluationDocumentModel>>> GetEvaluationDocument([AliasAs("Id")] long evaluationId);

    [Multipart]
    [Post("/evaluationDocument/{id}")]
    [Headers("Authorization: Bearer", "Accept:application/pdf")]
    Task<IApiResponse<EvaluationDocumentModel>> CreateEvaluationDocument([AliasAs("Id")] long evaluationId, [AliasAs("Form")] StreamPart streamPart, [Query] CreateEvaluationDocumentRequest createEvaluationDocumentRequest);
}


public class CreateEvaluationDocumentRequest
{
    public string DocumentType { get; set; }
    public string UserName { get; set; }
    public string ApplicationId { get; set; }
}