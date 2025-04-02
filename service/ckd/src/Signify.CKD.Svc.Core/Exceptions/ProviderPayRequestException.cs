using System;
using System.Net;
using System.Runtime.Serialization;
using Refit;

namespace Signify.CKD.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if there was an issue sending a request to the ProviderPay API
/// </summary>
[Serializable]
public class ProviderPayRequestException : Exception
{
    /// <summary>
    /// Identifier of the event that raised this exception
    /// </summary>
    public int CkdId { get; }
    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; }
    /// <summary>
    /// HTTP status code received from the ProviderPay API
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    public ProviderPayRequestException(int ckdId, long evaluationId, HttpStatusCode statusCode, string message, ApiException innerException = null)
        : base($"{message} for CkdId={ckdId}, EvaluationId={evaluationId}, with StatusCode={statusCode}", innerException)
    {
        CkdId = ckdId;
        EvaluationId = evaluationId;
        StatusCode = statusCode;
    }

    #region ISerializable
    protected ProviderPayRequestException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        CkdId = info.GetInt32("CkdId")!;
        EvaluationId = info.GetInt64("EvaluationId");
        StatusCode = (HttpStatusCode) info.GetInt32("StatusCode");
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);

        info.AddValue("CkdId", CkdId.ToString(), typeof(string));
        info.AddValue("EvaluationId", EvaluationId, typeof(long));
        info.AddValue("StatusCode", (int)StatusCode, typeof(int));
    }
    #endregion ISerializable
}

