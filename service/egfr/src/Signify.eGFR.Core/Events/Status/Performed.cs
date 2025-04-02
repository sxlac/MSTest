namespace Signify.eGFR.Core.Events.Status;

/// <summary>
/// Status event signifying that an evaluation with eGFR product
/// had a eGFR exam performed
/// </summary>
public class Performed : BaseStatusMessage
{
    public string Barcode { get; set; }
}