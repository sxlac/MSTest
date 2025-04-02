using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.Events.Status;

/// <summary>
/// Status event signifying that an evaluation with uACR product
/// had a uACR exam performed
/// </summary>
[ExcludeFromCodeCoverage]
public class Performed : BaseStatusMessage
{
    public string Barcode { get; set; }
}