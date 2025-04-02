using System;
using System.Collections.Generic;

namespace Signify.DEE.Messages;

/// <summary>
/// Result details published to Kafka
/// </summary>
public class Result
{
    /// <summary>
    /// Signify Product Code for this event
    /// </summary>
    public string ProductCode { get; set; }

    /// <summary>
    /// Unique identifier of the evaluation this event corresponds to
    /// </summary>
    public long EvaluationId { get; set; }

    /// <summary>
    /// UTC timestamp this evaluation was finalized on the provider's iPad (not necessarily when the Signify
    /// Evaluation API received the message, for ex in the case of the iPad being offline)
    /// </summary>
    public DateTimeOffset PerformedDate { get; set; }

    /// <summary>
    /// UTC timestamp results for this product and evaluation were received by this process manager
    /// </summary>
    public DateTimeOffset ReceivedDate { get; set; }
    
    /// <summary>
    /// UTC timestamp when interpretation/grading was completed.
    /// </summary>
    public DateTimeOffset? DateGraded { get; set; }

    /// <summary>
    /// Wether or not this is a billable event
    /// </summary>
    public bool IsBillable { get; set; }

    /// <summary>
    /// ie Normality/Abnormality Indicator
    /// </summary>
    /// <remarks>
    ///	Possible values are "N" (normal), "A" (abnormal), "U" (undetermined)
    ///
    /// Value should be the same as <see cref="Results"/>.AbnormalIndicator
    /// </remarks>
    public string Determination { get; set; }

    /// <summary>
    /// Information about the person that graded the images
    /// </summary>
    public Grader Grader { get; set; }

    public string CarePlan { get; set; }

    public ICollection<string> DiagnosisCodes { get; set; } = new List<string>();

    /// <summary>
    /// Exam results for each side (eye)
    /// </summary>
    public ICollection<SideResultInfo> Results { get; set; } = new List<SideResultInfo>();
}

/// <summary>
/// Information about a person that grades images
/// </summary>
public class Grader
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    /// <summary>
    /// Grader's National Provider Identifier
    /// </summary>
    public string NPI { get; set; }

    /// <summary>
    /// A taxonomy code is a unique 10-character code that designates the grader's classification and specialization
    /// </summary>
    public string Taxonomy { get; set; }
}

/// <summary>
/// Result details for one side (eye)
/// </summary>
public class SideResultInfo
{
    #region Will not be serialized
    public const string LeftSide = "L";
    public const string RightSide = "R";
    #endregion

    /// <remarks>
    /// Either 'L' or 'R'
    /// </remarks>
    public string Side { get; set; }

    /// <summary>
    /// Whether or not there was at least one image for this side (eye) that was gradable
    /// </summary>
    public bool Gradable { get; set; }

    /// <remarks>
    /// 'N' - Normal
    /// 'A' - Abnormal
    /// 'U' - Undetermined
    /// </remarks>
    public string AbnormalIndicator { get; set; }

    /// <summary>
    /// Whether or not pathology is present for this side
    /// </summary>
    public bool? Pathology { get; set; }

    /// <summary>
    /// Findings for this side (eye)
    /// </summary>
    public ICollection<SideFinding> Findings { get; set; } = new List<SideFinding>();

    /// <summary>
    /// Zero or more reasons why image(s) for this side were not gradable
    /// </summary>
    public ICollection<string> NotGradableReasons { get; set; } = new List<string>();
}

/// <summary>
/// Details about a finding for an eye
/// </summary>
public class SideFinding
{
    /// <summary>
    /// Name of the finding, such as Diabetic Retinopathy
    /// </summary>
    public string Finding { get; set; }

    /// <summary>
    /// Result of the finding, such as None, or Suspected Epiretinal Membrane
    /// </summary>
    public string Result { get; set; }

    /// <remarks>
    /// 'N' - Normal
    /// 'A' - Abnormal
    /// 'U' - Undetermined
    /// </remarks>
    public string AbnormalIndicator { get; set; }
}