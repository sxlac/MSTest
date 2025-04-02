using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Constants.Questions;

/// <summary>
/// PAD Diagnosis confirmed based on satisfied clinical criteria above?
/// This is a question that will prevent the performed/not performed question from showing up.
/// </summary>
[ExcludeFromCodeCoverage]
public static class PadDiagnosisConfirmedClinicallyQuestion
{
    public const int QuestionId = 100660;

    public const string QuestionText = "PAD Diagnosis confirmed based on satisfied clinical criteria above?";
    /// <summary>
    /// Yes
    /// </summary>
    public const int YesAnswerId = 52831;

    /// <summary>
    /// No
    /// </summary>
    public const int NoAnswerId = 52832;

    public const string Reason = "Diagnosis can be confirmed without test";
}