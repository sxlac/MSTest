namespace Signify.eGFR.Core.Events.Status;

/// <summary>
/// Status event signifying that an evaluation with eGFR product
/// did not have a eGFR exam performed
/// </summary>
public class NotPerformed : BaseStatusMessage
{
    public string ReasonType { get; set; }
    public string Reason { get; set; }
    public string ReasonNotes { get; set; }
}