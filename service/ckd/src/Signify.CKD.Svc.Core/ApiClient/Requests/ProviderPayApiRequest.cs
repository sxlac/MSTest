using System.Collections.Generic;

namespace Signify.CKD.Svc.Core.ApiClient.Requests;

public class ProviderPayApiRequest
{
    public long ProviderId { get; set; }
    public string ProviderProductCode { get; set; }
    public string PersonId { get; set; }
    public string DateOfService { get; set; }
    public int ClientId { get; set; }
    public Dictionary<string, string> AdditionalDetails { get; set; }
}