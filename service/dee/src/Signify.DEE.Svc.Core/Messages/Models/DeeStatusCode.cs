using System;

namespace Signify.DEE.Svc.Core.Messages.Models;

public abstract class DeeStatusCode
{
    public string ProductCode => Constants.ApplicationConstants.ProductCode;
    public long EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTimeOffset ReceivedDateTime { get; set; }
}