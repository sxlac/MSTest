using System;

namespace Signify.CKD.Svc.Core.ApiClient.Requests
{
    public class  UpdateInventoryRequest
	{
		public Guid RequestId { get; set; }
		public string ItemNumber { get; set; }
		public DateTime DateUpdated { get; set; }
		public string SerialNumber { get; set; }
		public int Quantity { get; set; }
		public int ProviderId { get; set; }
		public string CustomerNumber { get; set; }
		public DateTime? ExpirationDate { get; set; }
	}
}
