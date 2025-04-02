using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public class BusinessRuleStatus
{
    public bool IsMet { get; set; }
    public string Reason { get; set; }
}