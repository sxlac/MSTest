using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;

/// <summary>
/// Represents a revision (or initial version) of an evaluation.
/// </summary>
/// <remarks>
/// This is a subset of the full model, which can be found at
/// https://dev.azure.com/signifyhealth/HCC/_git/coreservices?path=/api/signify.evaluationapi.webapi/src/Signify.EvaluationsApi.Core/Models/EvaluationVersion.cs
/// </remarks>
[ExcludeFromCodeCoverage]
public class EvaluationVersion
{
    /// <summary>
    /// Version of the evaluation answers. This value starts as `1` the
    /// first time the evaluation is finalized, and gets incremented each
    /// time it is finalized again.
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
[ExcludeFromCodeCoverage]
public class EvaluationModel
{
    /// <summary>
    /// Version of the Form this evaluation corresponds to.
    /// </summary>
    /// <remarks>
    /// When an evaluation is created (ie Started), the version of the form
    /// that is latest on the provider's device is used. This form version
    /// associated with an evaluation when it is created cannot change, even
    /// if the provider were to upgrade their device and get a newer version
    /// or they were to re-finalize the evaluation numerous times. This is
    /// to ensure the questions and answers associated with an evaluation
    /// are fixed and cannot change once it has started.
    /// <br /><br />
    /// Note, the form version identifier that gets incremented each time a
    /// new form gets published is a shared identifier across all the
    /// various form types (ex IHE, VIHE, Medicaid, Pediatric, etc). This
    /// means if there is a FormVersionId value of 1 that is tied to
    /// IHE/HHRA, and a new form is created for VIHE/VHRA it will have a
    /// value of 2. If another IHE/HHRA form is created, it will then have
    /// a value of 3.
    /// </remarks>
    public int FormVersionId { get; set; }

    /// <summary>
    /// Set of answers the user provided during the evaluation
    /// </summary>
    public List<EvaluationAnswerModel> Answers { get; set; } = new List<EvaluationAnswerModel>();
}

/// <remarks>
/// This is a subset of the full model, which can be found at
/// https://dev.azure.com/signifyhealth/HCC/_git/coreservices?path=/api/signify.evaluationapi.webapi/src/Signify.EvaluationsApi.Core/Models/EvaluationAnswer.cs
/// </remarks>
[ExcludeFromCodeCoverage]
public class EvaluationAnswerModel
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