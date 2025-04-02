using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions;

/// <summary>
/// Exception that can be thrown in an NSB event handler if a feature is disabled and
/// you do not want to lose the event, but instead, leave it in the NSB error queue
/// until the feature is (re-)enabled.
/// </summary>
[Serializable]
public sealed class FeatureDisabledException : Exception
{
    public long EvaluationId { get; }

    public string FeatureName { get; }

    public FeatureDisabledException(long evaluationId, string featureName)
        : base($"Feature {featureName} is disabled, not processing event for EvaluationId={evaluationId}")
    {
        EvaluationId = evaluationId;
        FeatureName = featureName;
    }

    [ExcludeFromCodeCoverage]
    #region ISerializable
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    private FeatureDisabledException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
    #endregion ISerializable
}