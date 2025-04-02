using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Constants.Questions.NotPerformed;

/// <summary>
/// Reason peripheral arterial disease testing not performed
/// </summary>
[ExcludeFromCodeCoverage]
public static class ReasonNotPerformedQuestion
{
    public const int QuestionId = 90695;

    public const int MemberRefusedAnswerId = 30957;

    public const int UnableToPerformAnswerId = 30958;

    public const int NotClinicallyRelevantAnswerId = 31125;

    public const int ReasonNotClinicallyRelevantNotesAnswerId = 31126;
}