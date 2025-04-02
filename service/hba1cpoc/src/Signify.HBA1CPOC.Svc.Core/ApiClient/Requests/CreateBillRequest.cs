using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;

[ExcludeFromCodeCoverage]
public class CreateBillRequest
{
    public int ClientId { get; set; }
    public int SharedClientId { get; set; }
    public int MemberPlanId { get; set; }
    public DateTime DateOfService { get; set; }
    public string UsStateOfService { get; set; }
    public int ProviderId { get; set; }
    public string RcmProductCode { get; set; }
    public string ApplicationId { get; set; }
    public DateTime BillableDate { get; set; }
    public Dictionary<string, string> AdditionalDetails { get; set; }
    public string CorrelationId { get; set; }
}