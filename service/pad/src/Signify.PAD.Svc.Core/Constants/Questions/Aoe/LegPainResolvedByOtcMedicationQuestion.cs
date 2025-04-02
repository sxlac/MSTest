using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Constants.Questions.Aoe;

/// <summary>
/// Atherosclerosis of Extremities question for members if their leg pain is resolved by taking over the counter medication
/// </summary>
[ExcludeFromCodeCoverage]
public static class LegPainResolvedByOtcMedicationQuestion
{
    public const int QuestionId = 100511;

    public const string QuestionText = "Does the pain go away after taking over the counter pain medication?";

    /// <summary>
    /// Yes
    /// </summary>
    public const int YesAnswerId = 52184;

    /// <summary>
    /// No
    /// </summary>
    public const int NoAnswerId = 52185;
}
