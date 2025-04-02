using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.ApiClient.Response;

[ExcludeFromCodeCoverage]
public class UpdateInventoryResponse
{
	public Guid RequestId { get; set; }
	public bool Success { get; set; }
	public string Message { get; set; }

}