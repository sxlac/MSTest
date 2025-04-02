using Signify.PAD.Svc.Core.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Models;

/// <summary>
/// Results of the Atherosclerosis of Extremities (AOE) questions
/// </summary>
[ExcludeFromCodeCoverage]
public class AoeResult
{
    public AoeResult()
    {
        ClinicalSupport = new List<ClinicalSupport>();
    }

    /// <summary>
    /// The Product Code Representing PAD
    /// </summary>
    public string ProductCode { get; } = Application.ProductCode;

    /// <summary>
    /// Identifier of the evaluation to be associated with this result
    /// </summary>
    public int EvaluationId { get; set; }

    /// <summary>
    /// Date and time the result was received
    /// </summary>
    public DateTimeOffset ReceivedDate { get; set; }

    /// <summary>
    /// Collection of all of the Clinical Support answerse
    /// </summary>
    public List<ClinicalSupport> ClinicalSupport { get; set; }
}
