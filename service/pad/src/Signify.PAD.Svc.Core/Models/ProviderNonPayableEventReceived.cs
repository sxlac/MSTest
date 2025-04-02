using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public class ProviderNonPayableEventReceived : ProviderPayableEventReceived
{
    /// <summary>
    /// Reason as to why the Exam is non payable
    /// </summary>
    public string Reason { get; set; }
}