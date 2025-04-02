using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.eGFR.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class ProviderPay
{
    public int ProviderPayId { get; set; }
    public string PaymentId { get; set; }
    public int ExamId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public virtual Exam Exam { get; set; }

    public override string ToString() =>
        $"{nameof(ProviderPayId)}: {ProviderPayId}, {nameof(PaymentId)}: {PaymentId}, {nameof(ExamId)}: {ExamId}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
}