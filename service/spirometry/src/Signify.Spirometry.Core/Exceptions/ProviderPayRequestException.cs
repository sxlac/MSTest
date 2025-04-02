using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.Serialization;
using Refit;

namespace Signify.Spirometry.Core.Exceptions;

/// <summary>
/// Exception raised if there was an issue sending a request to the ProviderPay API
/// </summary>
[Serializable]
public sealed class ProviderPayRequestException : Exception
{
    /// <summary>
    /// Identifier of the event that raised this exception
    /// </summary>
    public int ExamId { get; }
    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; }
    /// <summary>
    /// HTTP status code received from the ProviderPay API
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    public ProviderPayRequestException(int examId, long evaluationId, HttpStatusCode statusCode, string message, ApiException innerException = null)
        : base($"{message} for ExamId={examId}, EvaluationId={evaluationId}, with StatusCode={statusCode}", innerException)
    {
        ExamId = examId;
        EvaluationId = evaluationId;
        StatusCode = statusCode;
    }

    [ExcludeFromCodeCoverage]
    #region ISerializable
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    private ProviderPayRequestException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
    #endregion ISerializable
}