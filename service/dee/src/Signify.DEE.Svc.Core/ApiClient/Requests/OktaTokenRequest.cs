using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.ApiClient.Requests;

[ExcludeFromCodeCoverage]
public class OktaTokenRequest
{
	public string grant_type { get; set; }
	public string scope { get; set; }

	/// <summary>
	/// This is the string format required by the Okta API.
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return $"grant_type={grant_type}&scope={scope}";
	}
}