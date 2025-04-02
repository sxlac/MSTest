using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Signify.Spirometry.Core.Models;

/// <summary>
/// Results of a Spirometry test that was performed, but with some result values not of
/// their actual data types or validated against valid values. Instead, contains the raw
/// answer values from the Evaluation API.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RawExamResult : IEquatable<RawExamResult>
{
    /// <summary>
    /// Identifier of the evaluation that contained this Spirometry exam
    /// </summary>
    public int EvaluationId { get; set; }

    /// <summary>
    /// The accuracy/gradability of this exam. Exams with low confidence cannot accurately diagnose COPD.
    /// </summary>
    public SessionGrade? SessionGrade { get; set; }

    /// <summary>
    /// Forced Vital Capacity - This is the largest amount of air that you can forcefully exhale after breathing
    /// in as deeply as you can. A lower than normal FVC reading indicates restricted breathing.
    /// </summary>
    /// <remarks>
    /// This is the raw, unvalidated <see cref="string"/> answer value from the Evaluation API
    /// </remarks>
    public string Fvc { get; set; }

    /// <summary>
    /// Forced Expiratory Volume per 1 second - How much air you can force from your lungs in one second. This
    /// reading helps your doctor assess the severity of your breathing problems. Lower FEV-1 readings indicate
    /// more significant obstruction.
    /// </summary>
    /// <remarks>
    /// This is the raw, unvalidated <see cref="string"/> answer value from the Evaluation API
    /// </remarks>
    public string Fev1 { get; set; }

    /// <summary>
    /// (Forced Expiratory Volume per 1 second) / (Forced Vital Capacity) - You can use this ratio to determine
    /// how long it takes to expend air from your lungs after breathing in as deeply as you can. This is done by
    /// dividing 1 by this number.
    /// </summary>
    /// <remarks>
    /// This is the raw, unvalidated <see cref="string"/> answer value from the Evaluation API
    /// </remarks>
    public string Fev1FvcRatio { get; set; }

    /// <summary>
    /// Whether the member has a high level of symptom support (at least 1 or more of the following:
    /// dyspnea, chronic cough -- productive or not, chronic sputum/phlegm/mucus production, wheezing,
    /// expiration &gt; inspiration - pursed lip breathing, OR cyanosis)
    /// </summary>
    /// <remarks>Null if provider did not answer the question</remarks>
    public TrileanType? HasHighSymptom { get; set; }

    /// <summary>
    /// Whether the member has environmental or exposure risk factors (at least 1 or more of the following:
    /// tobacco use, other inhaled drugs, occupational environmental exposure -- organic and inorganic dust,
    /// chemical irritant/fumes, open flame heating/cooking OR high levels or urban pollution exposure)
    /// </summary>
    /// <remarks>Null if provider did not answer the question</remarks>
    public TrileanType? HasEnvOrExpRisk { get; set; }

    /// <summary>
    /// Whether the member has a high level of comorbidity support (at least 1 or more of the following:
    /// history of recurrent lower respiratory tract infections, lung cancer, reactive airway disease,
    /// history of respiratory failure, history of chronic conditions associated with respiratory arrest)
    /// </summary>
    /// <remarks>Null if provider did not answer the question</remarks>
    public TrileanType? HasHighComorbidity { get; set; }

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
    /// How often the member coughs up mucus
    /// </summary>
    public OccurrenceFrequency? CoughMucusFrequency { get; set; }

    /// <summary>
    /// Whether the member has had wheezing in the past 12 months
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public TrileanType? HadWheezingPast12mo { get; set; }

    /// <summary>
    /// Whether the member gets short of breath at rest
    /// </summary>
    public TrileanType? GetsShortnessOfBreathAtRest { get; set; }

    /// <summary>
    /// Whether the member gets short of breath with mild exertion
    /// </summary>
    public TrileanType? GetsShortnessOfBreathWithMildExertion { get; set; }

    /// <summary>
    /// How often the member's chest sounds noisy (wheezy, whistling, rattling) when they breathe
    /// </summary>
    public OccurrenceFrequency? NoisyChestFrequency { get; set; }

    /// <summary>
    /// How often the member experiences shortness of breath during physical activity (walking up
    /// a flight of stairs or walking up an incline without stopping to rest)
    /// </summary>
    public OccurrenceFrequency? ShortnessOfBreathPhysicalActivityFrequency { get; set; }

    /// <summary>
    /// A calculation of COPD risk, which is calculated based on symptom support questions, along
    /// with the member's age and number of years the member smoked, when applicable
    /// </summary>
    /// <remarks>
    /// A score of 18 or less (29 is the current maximum) indicates a member is at risk for COPD.
    /// Prior history of COPD, the Lung Function Questionnaire Score, along with the Spirometry test
    /// are utilized in the diagnosis of COPD.
    /// </remarks>
    public int? LungFunctionQuestionnaireScore { get; set; }

    /// <summary>
    /// The resulting diagnosis of whether the member has COPD (Chronic Obstructive Pulmonary Disease).
    /// Note, session grades "D, E, F" are poor quality and COPD cannot be diagnosed based on Spirometry results.
    /// </summary>
    /// <remarks>
    /// Value will either be TRUE or null; cannot be FALSE
    /// </remarks>
    public bool? CopdDiagnosis { get; set; }

    /// <summary>
    /// List of diagnoses, many of which are irrelevant to Spirometry, that either Signify
    /// has claimed from a previous IHE with the member, and/or diagnoses claimed by the
    /// member's healthcare plan.
    /// </summary>
    public ICollection<string> PreviousDiagnoses { get; set; } = new List<string>();

    #region IEquatable
    public bool Equals(RawExamResult other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EvaluationId == other.EvaluationId
               && SessionGrade == other.SessionGrade
               && Fvc == other.Fvc
               && Fev1 == other.Fev1
               && Fev1FvcRatio == other.Fev1FvcRatio
               && HasHighSymptom == other.HasHighSymptom
               && HasEnvOrExpRisk == other.HasEnvOrExpRisk
               && HasHighComorbidity == other.HasHighComorbidity
               && HasSmokedTobacco == other.HasSmokedTobacco
               && TotalYearsSmoking == other.TotalYearsSmoking
               && ProducesSputumWithCough == other.ProducesSputumWithCough
               && CoughMucusFrequency == other.CoughMucusFrequency
               && HadWheezingPast12mo == other.HadWheezingPast12mo
               && GetsShortnessOfBreathAtRest == other.GetsShortnessOfBreathAtRest
               && GetsShortnessOfBreathWithMildExertion == other.GetsShortnessOfBreathWithMildExertion
               && NoisyChestFrequency == other.NoisyChestFrequency
               && ShortnessOfBreathPhysicalActivityFrequency == other.ShortnessOfBreathPhysicalActivityFrequency
               && LungFunctionQuestionnaireScore == other.LungFunctionQuestionnaireScore
               && CopdDiagnosis == other.CopdDiagnosis
               && !PreviousDiagnoses.Except(other.PreviousDiagnoses).Any();
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType()
               && Equals((RawExamResult) obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(EvaluationId);
        hashCode.Add(SessionGrade);
        hashCode.Add(Fvc);
        hashCode.Add(Fev1);
        hashCode.Add(Fev1FvcRatio);
        hashCode.Add(HasHighSymptom);
        hashCode.Add(HasEnvOrExpRisk);
        hashCode.Add(HasHighComorbidity);
        hashCode.Add(HasSmokedTobacco);
        hashCode.Add(TotalYearsSmoking);
        hashCode.Add(ProducesSputumWithCough);
        hashCode.Add(CoughMucusFrequency);
        hashCode.Add(HadWheezingPast12mo);
        hashCode.Add(GetsShortnessOfBreathAtRest);
        hashCode.Add(GetsShortnessOfBreathWithMildExertion);
        hashCode.Add(NoisyChestFrequency);
        hashCode.Add(ShortnessOfBreathPhysicalActivityFrequency);
        hashCode.Add(LungFunctionQuestionnaireScore);
        hashCode.Add(CopdDiagnosis);
        hashCode.Add(PreviousDiagnoses.GetHashCode());
        return hashCode.ToHashCode();
    }

    public static bool operator ==(RawExamResult left, RawExamResult right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RawExamResult left, RawExamResult right)
    {
        return !Equals(left, right);
    }
    #endregion IEquatable
}