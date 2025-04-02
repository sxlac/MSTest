using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class ProviderPay
{
    public int ProviderPayId { get; set; }
    public int SpirometryExamId { get; set; }
    public string PaymentId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }

    public virtual SpirometryExam SpirometryExam { get; set; }
}