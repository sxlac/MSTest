using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Messages.Events.Status;

[ExcludeFromCodeCoverage]
public class Performed : BaseStatusMessage
{
    public string Barcode { get; set; }
}