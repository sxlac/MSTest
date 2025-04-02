using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public class BusinessRuleStatus
{
    public bool IsMet { get; set; }
    public string Reason { get; set; }
}