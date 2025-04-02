using System;

namespace Signify.CKD.Svc.Core.ApiClient.Response
{
    public class UpdateInventoryResponse
	{
		public Guid RequestId { get; set; }
		public bool Success { get; set; }
		public string Message { get; set; }

	}
}
