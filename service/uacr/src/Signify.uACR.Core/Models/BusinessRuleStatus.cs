using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.Models;

[ExcludeFromCodeCoverage]
public class BusinessRuleStatus
{
    public bool IsMet { get; set; }
    public string Reason { get; set; }
}