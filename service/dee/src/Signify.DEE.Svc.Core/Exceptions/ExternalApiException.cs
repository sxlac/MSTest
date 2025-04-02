using System;

namespace Signify.DEE.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if call to an external API fails
/// </summary>
[Serializable]
public class ExternalApiException(long evaluationId, Guid eventId, string apiName) : Exception(
    $"Exception while accessing {apiName} APIUnable to find an exam with EvaluationId={evaluationId}, for EventId={eventId}")
{
    public Guid EventId { get; } = eventId;
    public long EvaluationId { get; } = evaluationId;
    public string ApiName { get; } = apiName;
}