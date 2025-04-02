using System;

namespace Signify.A1C.Svc.Core.ApiClient.Response
{
    public class UpdateInventoryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid RequestId { get; set; }
    }
}