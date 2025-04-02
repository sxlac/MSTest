using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Events.Status;

[ExcludeFromCodeCoverage]
public class ProviderPayRequestSent : BaseStatusMessage
{
#pragma warning disable CA1822
    public string ProviderPayProductCode => Constants.ProductCodes.Spirometry;
#pragma warning restore CA1822
    public string PaymentId { get; set; }
    public DateTimeOffset ParentEventDateTime { get; set; }
}