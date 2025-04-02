using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Configs;

[ExcludeFromCodeCoverage]
public class IrisDocumentInfoConfig
{
    public string ApplicationId { get; set; }
    public string DocumentType { get; set; }
    public string UserName { get; set; }
}