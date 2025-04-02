using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Events.Status;

/// <summary>
/// Status event signifying that an evaluation with Spiro product did not have a Spiro exam performed
/// </summary>
[ExcludeFromCodeCoverage]
public class NotPerformed : BaseStatusMessage
{
    public string ReasonType { get; set; }
    public string Reason { get; set; }
    public string ReasonNotes { get; set; }
}