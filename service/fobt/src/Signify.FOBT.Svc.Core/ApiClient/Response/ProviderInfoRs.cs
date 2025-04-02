using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.ApiClient.Response;

[ExcludeFromCodeCoverage]
public class ProviderInfoRs
{
	public int ProviderId { get; set; }
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public string NationalProviderIdentifier { get; set; }
}