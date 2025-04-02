using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Constants.Questions;

/// <summary>
/// Peripheral arterial disease testing performed today?
/// </summary>
[ExcludeFromCodeCoverage]
public static class PadTestPerformedQuestion
{
    public const int QuestionId = 90572;

    public const string QuestionText = "Peripheral arterial disease testing performed today?";
    /// <summary>
    /// Yes
    /// </summary>
    public const int YesAnswerId = 29560;

    /// <summary>
    /// No
    /// </summary>
    public const int NoAnswerId = 29561;
}