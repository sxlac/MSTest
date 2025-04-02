using System;

namespace Signify.DEE.Svc.Core.Data.Entities;

public class ProviderPay
{
    public ProviderPay()
    {
    }

    public int Id { get; set; }
    public string PaymentId { get; set; }

    public int ExamId { get; set; }

    //Foreign key
    public virtual Exam Exam { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }
    public string ProviderPayProductCode { get; set; }

    public override string ToString() =>
        $"{nameof(Id)}: {Id}, {nameof(PaymentId)}: {PaymentId}, {nameof(ExamId)}: {ExamId}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
}