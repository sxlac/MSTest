using System;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class DEEBillingModel
{
    public DEEBillingModel()
    {
    }

    public int Id { get; set; }
    public int ExamId { get; set; }
    public string BillId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
}