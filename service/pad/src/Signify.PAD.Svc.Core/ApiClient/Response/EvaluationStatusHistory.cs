using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.ApiClient.Response;

/// <summary>
/// Represents an object containing version details of an evaluation.
/// </summary>
/// <remarks>
/// Source - https://dev.azure.com/signifyhealth/HCC/_git/coreservices?path=/api/signify.evaluationapi.webapi/src/Signify.EvaluationsApi.Core/DTO/EvaluationVersionDto.cs
/// Swagger in UAT - https://coreapi.uat.signifyhealth.com/evaluation/swagger/index.html
/// </remarks>
/// <example>
/// {
///     "version": 1,
///     "createdDateTime": "2023-11-13T08:39:56.4870000",
///     "evaluationId": 288820,
///     "evaluationStatusCodeId": 3,
///     "evaluationStatusCode": {
///         "id": 3,
///         "name": "Finalize"
///     }
/// }
/// </example>
[ExcludeFromCodeCoverage]
public class EvaluationStatusHistory
{
    /// <summary>
    /// Version of the evaluation
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The creation date/time of this evaluation version.
    /// </summary>
    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// The associated EvaluationId.
    /// </summary>
    public long EvaluationId { get; set; }

    /// <summary>
    /// The number that uniquely identifies this status code.
    /// </summary>
    /// <remarks>
    /// 3 - Finalize
    /// 4 - Cancel
    /// </remarks>
    public int EvaluationStatusCodeId { get; set; }

    /// <summary>
    /// The details corresponding to this evaluation's status for the specified <see cref="Version"/>.
    /// </summary>
    public EvaluationStatusCode EvaluationStatusCode { get; set; } = new();
}

[ExcludeFromCodeCoverage]
public class EvaluationStatusCode
{
    /// <summary>
    /// The number that uniquely identifies this status code.
    /// </summary>
    /// <remarks>
    /// 3 - Finalize
    /// 4 - Cancel
    /// </remarks>
    public int Id { get; set; }

    /// <summary>
    /// The user friendly name representing this evaluation status.
    /// </summary>
    public string Name { get; set; }
}