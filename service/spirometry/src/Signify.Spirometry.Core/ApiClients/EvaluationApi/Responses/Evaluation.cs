using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;

/// <remarks>
/// This is a subset of the full model, which can be found at
/// https://dev.azure.com/signifyhealth/HCC/_git/coreservices?path=/api/signify.evaluationapi.webapi/src/Signify.EvaluationsApi.Core/Models/Evaluation.cs
/// </remarks>
[ExcludeFromCodeCoverage]
public class Evaluation
{
    /// <summary>
    /// Evaluation ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Appointment ID the evaluation is for
    /// </summary>
    public long AppointmentId { get; set; }
}