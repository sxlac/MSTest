using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public class ClinicalSupport
{
    public string SupportType { get; set; }

    public string SupportValue { get; set; }
}
