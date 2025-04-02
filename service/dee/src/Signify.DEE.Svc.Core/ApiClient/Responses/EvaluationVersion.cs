using System;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.ApiClient.Responses;

/// <summary>
/// Represents a revision (or initial version) of an evaluation.
/// </summary>
/// <remarks>
/// This is a subset of the full model, which can be found at
/// https://dev.azure.com/signifyhealth/HCC/_git/coreservices?path=/api/signify.evaluationapi.webapi/src/Signify.EvaluationsApi.Core/Models/EvaluationVersion.cs
/// </remarks>
public class EvaluationVersion
{
    /// <summary>
    /// Version of the evaluation
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Evaluation linked to this version
    /// </summary>
    public EvaluationModel Evaluation { get; set; }
}

/// <remarks>
/// This is a subset of the full model, which can be found at
/// https://dev.azure.com/signifyhealth/HCC/_git/coreservices?path=/api/signify.evaluationapi.webapi/src/Signify.EvaluationsApi.Core/Models/Evaluation.cs
/// </remarks>
public class EvaluationModel
{
    /// <summary>
    /// Set of answers the user provided during the evaluation
    /// </summary>
    public List<EvaluationAnswer> Answers { get; set; } = new();

    public int ProviderId { get; set; }

    public int MemberPlanId { get; set; }

    public DateTimeOffset? DateOfService { get; set; }
}

/// <remarks>
/// This is a subset of the full model, which can be found at
/// https://dev.azure.com/signifyhealth/HCC/_git/coreservices?path=/api/signify.evaluationapi.webapi/src/Signify.EvaluationsApi.Core/Models/EvaluationAnswer.cs
/// </remarks>
public class EvaluationAnswer
{
    /// <summary>
    /// Identifier of the question asked on the evaluation form
    /// </summary>
    public int QuestionId { get; set; }

    /// <summary>
    /// Identifier of the answer the provider selected
    /// </summary>
    /// <remarks>
    /// For a given question, there may be a limited pre-defined set of answers the provider may choose from.
    /// For example, "Yes"/"No" radio buttons. This <see cref="AnswerId"/> corresponds to the identifier of the
    /// answer selected.
    /// </remarks>
    public int AnswerId { get; set; }

    /// <summary>
    /// String representation of the answer value the provider selected to the given question on the evaluation form,
    /// or free-form answer value the provider entered in the case of a free-form text box
    /// </summary>
    public string AnswerValue { get; set; }
}