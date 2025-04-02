using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.ApiClient.Response;

[ExcludeFromCodeCoverage]
public class ProviderRs
{
	public int ProviderId { get; set; }
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public string NationalProviderIdentifier { get; set; }
}