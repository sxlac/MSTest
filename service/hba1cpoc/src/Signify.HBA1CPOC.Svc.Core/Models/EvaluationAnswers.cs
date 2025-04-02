using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public class EvaluationAnswers
{
    public bool IsHBA1CEvaluation { get; set; }
    public string A1CPercent { get; set; }
    public string NormalityIndicator { get; set; }
    public DateTime ExpirationDate { get; set; }
}