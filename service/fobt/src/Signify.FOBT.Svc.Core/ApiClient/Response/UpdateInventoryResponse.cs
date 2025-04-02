using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.ApiClient.Response;

[ExcludeFromCodeCoverage]
public class UpdateInventoryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Guid RequestId { get; set; }
}