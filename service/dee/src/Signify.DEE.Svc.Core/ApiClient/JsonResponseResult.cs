using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.ApiClient;

[ExcludeFromCodeCoverage]
public class JsonResponseResult<T>(T result = default(T), string errorMessage = null)
{
    //public string Version { get { return "1.2.3"; } }
    public bool Success => string.IsNullOrEmpty(ErrorMessage);
    public string ErrorMessage { get; set; } = errorMessage;
    public T Result { get; set; } = result;
}