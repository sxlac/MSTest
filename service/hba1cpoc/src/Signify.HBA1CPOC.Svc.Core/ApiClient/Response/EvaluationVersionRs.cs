using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.ApiClient.Response;

/// <summary>
/// Represents a revision (or initial version) of an evaluation.
/// </summary>
/// <remarks>
/// This is a subset of the full model, which can be found at
/// https://dev.azure.com/signifyhealth/HCC/_git/coreservices?path=/api/signify.evaluationapi.webapi/src/Signify.EvaluationsApi.Core/Models/EvaluationVersion.cs
/// </remarks>
[ExcludeFromCodeCoverage]
public class EvaluationVersionRs
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
[ExcludeFromCodeCoverage]
public class EvaluationModel
{
	/// <summary>
	/// Set of answers the user provided during the evaluation
	/// </summary>
	public List<EvaluationAnswerModel> Answers { get; set; }
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