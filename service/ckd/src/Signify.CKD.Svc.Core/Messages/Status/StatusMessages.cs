using Signify.CKD.Svc.Core.Constants;
using System;

namespace Signify.CKD.Svc.Core.Messages.Status;

/// <summary>
/// Base class for all status events published to Kafka
/// </summary>
public abstract class BaseStatusMessage
{
    public string ProductCode { get; } = Application.ProductCode;
    public int? EvaluationId { get; set; }
    public long? MemberPlanId { get; set; }
    public int? ProviderId { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
}

public class Performed : BaseStatusMessage
{ }

public class NotPerformed : BaseStatusMessage
{
    /// <summary>
    /// Gets or sets the "Member refused" or "Unable to perform" answer
    /// </summary>
    public string ReasonType { get; set; }

    /// <summary>
    /// Gets or sets the reason for not performing evaluation
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// Gets or sets the "member refused notes" or "Unable to perform notes" question
    /// </summary>
    public string ReasonNotes { get; set; }
}

public class BillRequestSent : BaseStatusMessage
{
    public string BillingProductCode { get; } = Application.ProductCode;
    public string BillId { get; set; }
    public DateTime? PdfDeliveryDate { get; set; }
}

public class BillRequestNotSent : BaseStatusMessage
{
    public string BillingProductCode { get; } = Application.ProductCode;
    public DateTime PdfDeliveryDate { get; set; }
}

public class ProviderPayRequestSent : BaseStatusMessage
{
    public string ProviderPayProductCode { get; set; }
    public string PaymentId { get; set; }
    public DateTime PdfDeliveryDate { get; set; }
}