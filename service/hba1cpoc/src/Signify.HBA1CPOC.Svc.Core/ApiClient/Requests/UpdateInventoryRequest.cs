using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;

[ExcludeFromCodeCoverage]
public class  UpdateInventoryRequest
{
	public Guid RequestId { get; set; }
	public string ItemNumber { get; set; }
	public DateTime DateUpdated { get; set; }
	public string SerialNumber { get; set; }
	public int Quantity { get; set; }
	public int ProviderId { get; set; }
	public string CustomerNumber { get; set; }
	public DateOnly? ExpirationDate { get; set; }
}