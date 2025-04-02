using System;

namespace Signify.CKD.Svc.Core.Data.Entities;

public class ProviderPay
{
    public int ProviderPayId { get; set; }
    public string PaymentId { get; set; }

    //Foreign key
    public virtual CKD CKD { get; set; }
    
    public int CKDId { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    public override string ToString() =>
        $"{nameof(ProviderPayId)}: {ProviderPayId}, {nameof(PaymentId)}: {PaymentId}, {nameof(CKD)}: {CKD}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
}