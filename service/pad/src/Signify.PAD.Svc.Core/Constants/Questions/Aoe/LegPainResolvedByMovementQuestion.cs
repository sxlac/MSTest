using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Constants.Questions.Aoe;

/// <summary>
/// Atherosclerosis of Extremities question for members if their leg pain being is resolved by movement
/// </summary>
[ExcludeFromCodeCoverage]
public static class LegPainResolvedByMovementQuestion
{
    public const int QuestionId = 100510;

    public const string QuestionText = "Does the pain go away after walking around for a few minutes or by hanging your feet over the edge of the bed?";

    /// <summary>
    /// Yes
    /// </summary>
    public const int YesAnswerId = 52182;

    /// <summary>
    /// No
    /// </summary>
    public const int NoAnswerId = 52183;
}
