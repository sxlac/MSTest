using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Constants.Questions.Aoe;

/// <summary>
/// Atherosclerosis of Extremities diagnosis confirmed based on PAD test results and clinical support
/// </summary>
[ExcludeFromCodeCoverage]
public static class AoeDiagnosisConfirmedQuestion
{
    public const int QuestionId = 100513;

    public const string QuestionText = "Atherosclerosis of extremities with resting leg pain diagnosis confirmed based on PAD test results and clinical support?";

    /// <summary>
    /// Confirmed
    /// </summary>
    public const int ConfirmedAnswerId = 52191;

    /// <summary>
    /// Not Confirmed
    /// </summary>
    public const int NotConfirmedAnswerId = 52192;
}
