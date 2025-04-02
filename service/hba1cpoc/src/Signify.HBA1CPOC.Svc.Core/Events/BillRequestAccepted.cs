using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Events;

[ExcludeFromCodeCoverage]
public class BillRequestAccepted
{
    public Guid RCMBillId { get; set; }

    public int? SharedClientID { get; set; }
    
    public DateTime DateOfService { get; set; }
    
    public long? MemberPlanId { get; set; }
    
    public string USStateOfService { get; set; }
    
    public long? ProviderId { get; set; }
    
    public string CorrelationId { get; set; }
    
    public string RCMProductCode { get; set; }
    
    public DateTime BillableDate { get; set; }
    
    public Guid? SubsidiaryId { get; set; }
    
    public Dictionary<string, string> AdditionalDetails { get; set; } = new();
    
    public string ApplicationId { get; set; }
}