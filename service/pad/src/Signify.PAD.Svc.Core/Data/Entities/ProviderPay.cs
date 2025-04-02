using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class ProviderPay
{
    public int ProviderPayId { get; set; }
    public string PaymentId { get; set; }

    //Foreign key
    public virtual PAD PAD { get; set; }
        
    public int PADId { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    public override string ToString()
        => $"{nameof(ProviderPayId)}: {ProviderPayId}, {nameof(PaymentId)}: {PaymentId}, {nameof(PADId)}: {PADId}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
}