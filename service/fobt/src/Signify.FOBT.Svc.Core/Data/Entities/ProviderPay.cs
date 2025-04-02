using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class ProviderPay
{
    public int Id { get; set; }
    public string PaymentId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public int FOBTId { get; set; }

    public override string ToString() =>
        $"{nameof(Id)}: {Id}, {nameof(PaymentId)}: {PaymentId}, {nameof(FOBTId)}: {FOBTId}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
}