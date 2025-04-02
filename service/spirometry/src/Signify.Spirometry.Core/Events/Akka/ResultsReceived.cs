using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Events.Akka;

/// <summary>
/// Event published to Kafka for downstream consumers and reporting
/// </summary>
[ExcludeFromCodeCoverage]
public class ResultsReceived
{
// Disable warning "does not access instance data and can be marked as static" - must be instance data for it to be serialized
#pragma warning disable CA1822
    public string ProductCode => Constants.ProductCodes.Spirometry;
#pragma warning restore CA1822

    public int EvaluationId { get; set; }
    /// <summary>
    /// UTC timestamp this exam was performed
    /// </summary>
    public DateTimeOffset PerformedDate { get; set; }
    /// <summary>
    /// UTC timestamp results were received for this exam
    /// </summary>
    public DateTimeOffset ReceivedDate { get; set; }
    /// <summary>
    /// Whether or not this exam qualifies being billed
    /// </summary>
    public bool IsBillable { get; set; }
    /// <summary>
    /// Overall normality/pathology determination of the results
    /// </summary>
    /// <remarks>Value range is of NormalityIndicator</remarks>
    public string Determination { get; set; }
    /// <summary>
    /// Exam result details
    /// </summary>
    public ExamResultInfo Results { get; set; }
}

[ExcludeFromCodeCoverage]
public class ExamResultInfo
{
    /// <summary>
    /// Grade of the session/test. This is an accuracy rating.
    /// </summary>
    /// <remarks>Possible values: "A"/"B"/"C"/"D"/"E"/"F" (or `null` if not answered)</remarks>
    public string SessionGrade { get; set; }
    /// <summary>
    /// FVC (Forced Vital Capacity)
    /// </summary>
    /// <remarks>May be `null` if the question was not answered</remarks>
    public int? Fvc { get; set; }
    /// <summary>
    /// Normality indicator for FVC (Forced Vital Capacity)
    /// </summary>
    public string FvcNormality { get; set; }
    /// <summary>
    /// FEV-1 (Forced Expiratory Volume per one second)
    /// </summary>
    /// <remarks>May be `null` if the question was not answered</remarks>
    public int? Fev1 { get; set; }
    /// <summary>
    /// Normality indicator for FEV-1 (Forced Expiratory Volume per one second)
    /// </summary>
    public string Fev1Normality { get; set; }
    /// <summary>
    /// FEV-1 / FVC ratio
    /// </summary>
    /// <remarks>May be `null` if the question was not answered</remarks>
    public decimal? Fev1OverFvc { get; set; }
    /// <summary>
    /// Whether the member has smoked tobacco
    /// </summary>
    /// <remarks>May be `null` if the question was not answered</remarks>
    public bool? HasSmokedTobacco { get; set; }
    /// <summary>
    /// Total number of years the member has smoked
    /// </summary>
    /// <remarks>May be `null` if the question was not answered</remarks>
    public int? TotalYearsSmoking { get; set; }
    /// <summary>
    /// Whether the member produces sputum when coughing
    /// </summary>
    /// <remarks>May be `null` if the question was not answered</remarks>
    public bool? ProducesSputumWithCough { get; set; }
    /// <summary>
    /// How often the member coughs up mucus
    /// </summary>
    /// <remarks>Possible values: "Never"/"Rarely"/"Sometimes"/"Often"/"Very often" (or `null` if not answered)</remarks>
    public string CoughMucusOccurrenceFrequency { get; set; }
    /// <summary>
    /// Whether the member has had wheezing in the past 12 months
    /// </summary>
    /// <remarks>Possible values: "Y"/"N"/"U" (for yes/no/undetermined) (or `null` if not answered)</remarks>
    // ReSharper disable once InconsistentNaming
    public string HadWheezingPast12mo { get; set; }
    /// <summary>
    /// Whether the member gets short of breath at rest
    /// </summary>
    /// <remarks>Possible values: "Y"/"N"/"U" (for yes/no/undetermined) (or `null` if not answered)</remarks>
    public string GetsShortnessOfBreathAtRest { get; set; }
    /// <summary>
    /// Whether the member gets short of breath with mild exertion
    /// </summary>
    /// <remarks>Possible values: "Y"/"N"/"U" (for yes/no/undetermined) (or `null` if not answered)</remarks>
    public string GetsShortnessOfBreathWithMildExertion { get; set; }
    /// <summary>
    /// How often the member's chest sounds noisy (wheezy, whistling, rattling) when they breathe
    /// </summary>
    /// <remarks>Possible values: "Never"/"Rarely"/"Sometimes"/"Often"/"Very often" (or `null` if not answered)</remarks>
    public string NoisyChestOccurrenceFrequency { get; set; }
    /// <summary>
    /// How often the member experiences shortness of breath during physical activity (walking up
    /// a flight of stairs or walking up an incline without stopping to rest)
    /// </summary>
    /// <remarks>Possible values: "Never"/"Rarely"/"Sometimes"/"Often"/"Very often" (or `null` if not answered)</remarks>
    public string ShortnessOfBreathPhysicalActivityOccurrenceFrequency { get; set; }
    /// <summary>
    /// A calculation of COPD risk, which is calculated based on symptom support questions, along
    /// with the member's age and number of years the member smoked, when applicable
    /// </summary>
    /// <remarks>
    /// A score of 18 or less (29 is the current maximum) indicates a member is at risk for COPD.
    /// Prior history of COPD, the Lung Function Questionnaire Score, along with the Spirometry test
    /// are utilized in the diagnosis of COPD.
    ///
    /// May be `null` if not answered
    /// </remarks>
    public int? LungFunctionScore { get; set; }
    /// <summary>
    /// COPD Diagnosis
    /// </summary>
    /// <remarks>
    /// Will never be `false`. Will either be `true` or `null` if COPD diagnosis cannot be determined.
    /// </remarks>
    public bool? Copd { get; set; }
    /// <summary>
    /// Whether this spirometry exam is eligible to receive an overread from the vendor. Note not all
    /// exams eligible for overread will result in a system flag being created for a clarification to
    /// the provider, nor will all exams eligible for overread be held in CDI.
    /// </summary>
    public bool EligibleForOverread { get; set; }
    /// <summary>
    /// Whether this evaluation's Hold in CDI was kept in effect until expiration or receipt of an
    /// overread from the vendor
    /// </summary>
    public bool WasHeldForOverread { get; set; }
}