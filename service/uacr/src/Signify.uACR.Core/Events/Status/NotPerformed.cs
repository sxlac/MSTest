using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.Events.Status;

/// <summary>
/// Status event signifying that an evaluation with uACR product
/// did not have a uACR exam performed
/// </summary>
[ExcludeFromCodeCoverage]
public class NotPerformed : BaseStatusMessage
{
    public string ReasonType { get; set; }
    public string Reason { get; set; }
    public string ReasonNotes { get; set; }
}