using System;

#nullable disable

namespace Signify.DEE.Svc.Core.Data.Entities;

public partial class DEEBilling
{

    //private ExamStatus _currentStatus;
    public DEEBilling()
    {
    }

    public int Id { get; set; }
    public int ExamId { get; set; }
    public string BillId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }

    /// <summary>
    /// Whether the BillRequestAccepted event was received
    /// </summary>
    public bool? Accepted { get; set; }

    /// <summary>
    /// Date and time when the BillRequestSent table is updated with BillRequestAccepted event
    /// i.e. when the Accepted field is set to true
    /// </summary>
    public DateTimeOffset? AcceptedAt { get; set; }

    public string RcmProductCode { get; set; }

    public static DEEBilling Create(int examId, string billId, DateTimeOffset createdDateTime, bool? accepted, DateTimeOffset? acceptedAt)
    {
        var deeBilling = new DEEBilling
        {
            ExamId = examId,
            BillId = billId,
            CreatedDateTime = createdDateTime,
            Accepted = accepted,
            AcceptedAt = acceptedAt
        };

        return deeBilling;
    }
}