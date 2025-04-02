using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class ProviderPay
{
    public ProviderPay()
    {
    }

    public int ProviderPayId { get; set; }
    public string PaymentId { get; set; }

    //Foreign key
    public virtual HBA1CPOC HBA1CPOC { get; set; }
    public int HBA1CPOCId { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    public override string ToString() =>
        $"{nameof(ProviderPayId)}: {ProviderPayId}, {nameof(PaymentId)}: {PaymentId}, {nameof(HBA1CPOCId)}: {HBA1CPOCId}, {nameof(HBA1CPOC)}: {HBA1CPOC}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
}