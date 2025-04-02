using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Messages.Events.Status;

[ExcludeFromCodeCoverage]
public class NotPerformed : BaseStatusMessage
{
    /// <summary>
    /// Gets or sets the "Member refused" or "Unable to perform" answer
    /// </summary>
    public string ReasonType { get; set; }

    /// <summary>
    /// Gets or sets the reason for not performing evaluation
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// Gets or sets the "member refused notes" or "Unable to perform notes" question
    /// </summary>
    public string ReasonNotes { get; set; }
}