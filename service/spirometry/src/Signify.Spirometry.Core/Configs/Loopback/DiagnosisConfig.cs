using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Configs.Loopback;

/// <summary>
/// Configuration allowing us to easily add additional, or modify existing, diagnosis
/// answer values
/// </summary>
[ExcludeFromCodeCoverage]
public class DiagnosisConfig
{
    /// <summary>
    /// Short name of the diagnosis
    /// </summary>
    public string Name { get; init; }
    /// <summary>
    /// Form answer value text for this diagnosis
    /// </summary>
    public string AnswerValue { get; init; }
}