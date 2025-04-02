using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public abstract class BusinessRuleAnswers
{
    public string LeftNormalityIndicator { get; set; }
    public string RightNormalityIndicator { get; set; }
}