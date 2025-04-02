using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.ApiClient.Requests;

[ExcludeFromCodeCoverage]
public class EvaluationRequest
{
    public string DocumentType { get; set; }
    public string UserName { get; set; }
    public string ApplicationId { get; set; }
}