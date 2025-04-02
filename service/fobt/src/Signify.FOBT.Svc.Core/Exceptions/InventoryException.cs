using System;

namespace Signify.FOBT.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if there was an issue sending a request to the ProviderPay API
/// </summary>
public class InventoryException : Exception
{
    /// <summary>
    /// Corresponding exam's identifier
    /// </summary>
    public long FobtId { get; }

    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; }

    /// <summary>
    /// The Id of the currently processed message
    /// </summary>
    public string MessageId { get; }

    public InventoryException(long evaluationId, int fobtId, string messageId, string message, Exception innerException = null)
        : base($"{message} for EvaluationId={evaluationId}, MessageId={messageId}", innerException)
    {
        FobtId = fobtId;
        EvaluationId = evaluationId;
        MessageId = messageId;
    }
}