using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public class BusinessRuleStatus
{
    public bool IsMet { get; set; }
    public string Reason { get; set; }
}