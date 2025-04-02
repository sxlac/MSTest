using Refit;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions;

/// <summary>
/// Exception raised if there was an issue sending a save flag request to the CDI API
/// </summary>
[Serializable]
public sealed class CdiSaveFlagRequestException : Exception
{
    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; }

    /// <summary>
    /// HTTP status code received from the CDI API
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    public CdiSaveFlagRequestException(long evaluationId, HttpStatusCode statusCode, string message, ApiException innerException = null)
        : base($"{message} for EvaluationId={evaluationId}, with StatusCode={statusCode}", innerException)
    {
        EvaluationId = evaluationId;
        StatusCode = statusCode;
    }

    [ExcludeFromCodeCoverage]
    #region ISerializable
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    private CdiSaveFlagRequestException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
    #endregion ISerializable
}