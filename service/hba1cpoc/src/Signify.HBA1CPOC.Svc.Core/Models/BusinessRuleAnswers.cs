using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public class BusinessRuleAnswers
{
    public string A1CPercent { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public DateTime? DateOfService { get; set; }
    public Normality NormalityIndicator { get; set; }
}