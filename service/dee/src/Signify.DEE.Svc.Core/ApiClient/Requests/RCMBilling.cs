using System;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.ApiClient.Requests;

public class RCMBilling
{
    public int SharedClientId { get; set; }
    public int MemberPlanId { get; set; }
    public DateTimeOffset DateOfService { get; set; }
    public string UsStateOfService { get; set; }
    public int ProviderId { get; set; }
    public string RcmProductCode { get; set; }
    public string ApplicationId { get; set; }
    public DateTimeOffset BillableDate { get; set; }
    public Dictionary<string, string> AdditionalDetails { get; set; }
    public string CorrelationId { get; set; }
}