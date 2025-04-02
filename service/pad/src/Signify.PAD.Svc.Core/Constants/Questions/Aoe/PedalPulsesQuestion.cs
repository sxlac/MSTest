using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Constants.Questions.Aoe;

/// <summary>
/// Atherosclerosis of Extremities question for members on Pedal Pulses
/// </summary>
[ExcludeFromCodeCoverage]
public static class PedalPulsesQuestion
{
    public const int QuestionId = 100512;

    public const string QuestionText = "Pedal Pulses";

    /// <summary>
    /// Normal
    /// </summary>
    public const int NormalAnswerId = 52186;

    /// <summary>
    /// Abnormal-Left
    /// </summary>
    public const int AbnormalLeftAnswerId = 52187;

    /// <summary>
    /// Abnormal-Right
    /// </summary>
    public const int AbnormalRightAnswerId = 52188;

    /// <summary>
    /// Abnormal-Bilateral
    /// </summary>
    public const int AbronmalBilateralAnswerId = 52189;

    /// <summary>
    /// Not Performed
    /// </summary>
    public const int NotPerformedAnswerId = 52190;
}
