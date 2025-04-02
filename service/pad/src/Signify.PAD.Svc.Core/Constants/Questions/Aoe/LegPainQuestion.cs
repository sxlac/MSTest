using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Constants.Questions.Aoe;

/// <summary>
/// Atherosclerosis of Extremities question for members if they experience pain in one or both legs while resting with their feet elevated
/// </summary>
[ExcludeFromCodeCoverage]
public static class LegPainQuestion
{
    public const int QuestionId = 100509;

    public const string QuestionText = "Does the member have pain in one or both of their legs while resting with their feet elevated?";

    /// <summmary>
    /// Right
    /// </summary>
    public const int YesRightLegAnswerId = 52178;

    /// <summary>
    /// Left
    /// </summary>
    public const int YesLeftLegAnswerId = 52179;

    /// <summary>
    /// Both
    /// </summary>
    public const int YesBothLegsAnswerId = 52180;

    /// <summary>
    /// Neither
    /// </summary>
    public const int NeitherAnswerId = 52181;
}
