using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.ApiClient.Response;

/// <summary>
/// Details of Provider obtained via the ProviderAPI
/// </summary>
[ExcludeFromCodeCoverage]
public class ProviderRs
{
	public int ProviderId { get; set; }
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public string NationalProviderIdentifier { get; set; }
}