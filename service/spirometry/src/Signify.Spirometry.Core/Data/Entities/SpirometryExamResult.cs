using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Data.Entities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global - Virtual properties are used by EF
[ExcludeFromCodeCoverage]
public class SpirometryExamResult
{
    public int SpirometryExamResultsId { get; set; }
    public int SpirometryExamId { get; set; }
    public short? SessionGradeId { get; set; }
    /// <remarks>
    /// Although FVC is volumetric (measured as Liters), this value is actually a <i>percentage of predicted
    /// volume</i> according to the member's demographics (age, weight, height, etc).
    /// </remarks>
    public short? Fvc { get; set; }
    /// <summary>
    /// Normality (indicator) for the FVC result
    /// </summary>
    public short FvcNormalityIndicatorId { get; set; }
    /// <remarks>
    /// Although FVC is volumetric (measured as Liters), this value is actually a <i>percentage of predicted
    /// volume</i> according to the member's demographics (age, weight, height, etc).
    /// </remarks>
    public short? Fev1 { get; set; }
    /// <summary>
    /// Normality (indicator) for the FEV-1 result
    /// </summary>
    public short Fev1NormalityIndicatorId { get; set; }
    /// <summary>
    /// FEV-1/FVC point-of-care result
    /// </summary>
    public decimal? Fev1FvcRatio { get; set; }
    /// <summary>
    /// Overall normality (indicator) of the exam (ie normality of FEV-1/FVC)
    /// </summary>
    public short NormalityIndicatorId { get; set; }
    public short? HasHighSymptomTrileanTypeId { get; set; }
    public short? HasEnvOrExpRiskTrileanTypeId { get; set; }
    public short? HasHighComorbidityTrileanTypeId { get; set; }
    public bool? CopdDiagnosis { get; set; }
    public DateTime CreatedDateTime { get; set; }
    /// <summary>
    /// Whether or not the member has smoked tobacco
    /// </summary>
    public bool? HasSmokedTobacco { get; set; }
    /// <summary>
    /// Total number of years the member has smoked
    /// </summary>
    public int? TotalYearsSmoking { get; set; }
    /// <summary>
    /// Whether or not the member produces sputum when coughing
    /// </summary>
    public bool? ProducesSputumWithCough { get; set; }
    /// <summary>
    /// FK to <see cref="OccurrenceFrequency"/>, for how often the member coughs up mucus
    /// </summary>
    public short? CoughMucusOccurrenceFrequencyId { get; set; }
    /// <summary>
    /// FK to <see cref="TrileanType"/>, for whether the member has had wheezing in the past 12 months
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public short? HadWheezingPast12moTrileanTypeId { get; set; }
    /// <summary>
    /// FK to <see cref="TrileanType"/>, for whether the member gets short of breath at rest
    /// </summary>
    public short? GetsShortnessOfBreathAtRestTrileanTypeId { get; set; }
    /// <summary>
    /// FK to <see cref="TrileanType"/>, for whether the member gets short of breath with mild exertion
    /// </summary>
    public short? GetsShortnessOfBreathWithMildExertionTrileanTypeId { get; set; }
    /// <summary>
    /// FK to <see cref="OccurrenceFrequency"/>, for how often the member's chest sounds noisy (wheezy,
    /// whistling, rattling) when they breathe
    /// </summary>
    public short? NoisyChestOccurrenceFrequencyId { get; set; }
    /// <summary>
    /// FK to <see cref="OccurrenceFrequency"/>, for how often the member experiences shortness of breath
    /// during physical activity (walking up a flight of stairs or walking up an incline without stopping to rest)
    /// </summary>
    public short? ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId { get; set; }
    /// <summary>
    /// Lung Function Questionnaire Score; a calculation of COPD risk, which is calculated based on symptom
    /// support questions, along with the member's age and number of years the member smoked, when applicable
    /// </summary>
    /// <remarks>
    /// A score of 18 or less (29 is the current maximum) indicates a member is at risk for COPD. Prior history
    /// of COPD, the Lung Function Questionnaire Score, along with the Spirometry test are utilized in the diagnosis
    /// of COPD.
    /// </remarks>
    public int? LungFunctionScore { get; set; }
    /// <summary>
    /// Overread FEV-1/FVC result
    /// </summary>
    public decimal? OverreadFev1FvcRatio { get; set; }
    /// <summary>
    /// Whether the member has a history of COPD
    /// </summary>
    public bool? HasHistoryOfCopd { get; set; }

    public virtual OccurrenceFrequency CoughMucusOccurrenceFrequency { get; set; }
    public virtual NormalityIndicator Fev1NormalityIndicator { get; set; }
    public virtual NormalityIndicator FvcNormalityIndicator { get; set; }
    public virtual TrileanType GetsShortnessOfBreathAtRestTrileanType { get; set; }
    public virtual TrileanType GetsShortnessOfBreathWithMildExertionTrileanType { get; set; }
    // ReSharper disable once InconsistentNaming
    public virtual TrileanType HadWheezingPast12moTrileanType { get; set; }
    public virtual TrileanType HasEnvOrExpRiskTrileanType { get; set; }
    public virtual TrileanType HasHighComorbidityTrileanType { get; set; }
    public virtual TrileanType HasHighSymptomTrileanType { get; set; }
    public virtual OccurrenceFrequency NoisyChestOccurrenceFrequency { get; set; }
    public virtual NormalityIndicator NormalityIndicator { get; set; }
    public virtual SessionGrade SessionGrade { get; set; }
    public virtual OccurrenceFrequency ShortnessOfBreathPhysicalActivityOccurrenceFrequency { get; set; }
    public virtual SpirometryExam SpirometryExam { get; set; }
}