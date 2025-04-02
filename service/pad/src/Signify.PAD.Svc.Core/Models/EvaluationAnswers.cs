using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public class EvaluationAnswers
{
    /// <summary>
    /// True if the evaluation contains a PAD product, and PAD testing was performed as part of the evaluation
    /// </summary>
    public bool IsPadPerformedToday { get; set; }
    /// <summary>
    /// Parsed and validated Left Score value
    /// </summary>
    /// <remarks>
    /// Null if <see cref="LeftScoreAnswerValue"/> is malformed, or not within a valid range
    /// </remarks>
    public string LeftScore { get; set; }
    /// <summary>
    /// Raw unvalidated answer value for Left Score
    /// </summary>
    public string LeftScoreAnswerValue { get; set; }
    public string LeftSeverity { get; set; }
    public string LeftNormalityIndicator { get; set; }
    /// <summary>
    /// When <see cref="LeftScore"/> does not have a value, this will explain the reason (ex invalid answer format, or out of range)
    /// </summary>
    public string LeftException { get; set; }
    /// <summary>
    /// Parsed and validated Right Score value
    /// </summary>
    /// <remarks>
    /// Null if <see cref="RightScoreAnswerValue"/> is malformed, or not within a valid range
    /// </remarks>
    public string RightScore { get; set; }
    /// <summary>
    /// Raw unvalidated answer value for Right Score
    /// </summary>
    public string RightScoreAnswerValue { get; set; }
    public string RightSeverity { get; set; }
    public string RightNormalityIndicator { get; set; }
    /// <summary>
    /// When <see cref="RightScore"/> does not have a value, this will explain the reason (ex invalid answer format, or out of range)
    /// </summary>
    public string RightException { get; set; }
    public int? NotPerformedAnswerId { get; set; }
    public string NotPerformedReasonType { get; set; }
    public string NotPerformedReason { get; set; }
    public string NotPerformedNotes { get; set; }
    /// <summary>
    /// Answer values for Atherosclerosis of Extermities symptom
    /// </summary>
    public AoeSymptomAnswers AoeSymptomAnswers { get; set; }
}