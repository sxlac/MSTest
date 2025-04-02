using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Configs.Loopback;

/// <summary>
/// Configuration for the spirometry diagnostic loopback feature
/// </summary>
[ExcludeFromCodeCoverage]
public class LoopbackConfig
{
    /// <summary>
    /// Configuration section key for this config
    /// </summary>
    public const string Key = "Loopback";

    /// <summary>
    /// Whether or not diagnostic overreads should be processed
    /// </summary>
    public bool ShouldProcessOverreads { get; set; }

    /// <summary>
    /// Whether or not CDI flags should be created to send queries to providers for clarification
    /// </summary>
    public bool ShouldCreateFlags { get; set; }

    /// <summary>
    /// Whether or not to release evaluation holds in CDI
    /// </summary>
    public bool ShouldReleaseHolds { get; set; }

    /// <summary>
    /// Lung Function Questionnaire Score maximum value to still be considered High Risk
    /// </summary>
    public int HighRiskLfqScoreMaxValue { get; set; }

    /// <summary>
    /// First FormVersionId that supports Dx Loopback
    /// </summary>
    public int FirstSupportedFormVersionId { get; set; }

    /// <summary>
    /// Get the collection of diagnoses required for this process manager
    /// </summary>
    public IEnumerable<DiagnosisConfig> Diagnoses { get; set; }

    /// <summary>
    /// Format and text (which supports Markdown) used when creating a flag in CDI, which will
    /// be presented to the provider in a clarification
    /// </summary>
    /// <remarks>
    /// Although this supports Markdown, there are some limitations. Because this is a string and
    /// not an actual file, you need to insert your own line breaks using <c>\n</c> or a
    /// <c>&lt;br&gt;</c> tag. A more notable limitation is that, because the contents are all on
    /// a single line/string, Markdown does not support the creation of a list (ordered or
    /// unordered) unless it is at the start of the line. This means there is no way to create an
    /// actual Markdown list; you must manually manipulate the indentations using non-breaking
    /// spaces (<c>&amp;nbsp;</c>) followed by a dash (<c>-</c>) or symbol such as <c>•</c>.
    /// </remarks>
    public string FlagTextFormat { get; set; }

    /// <summary>
    /// Configurations around looking up the evaluation associated with an overread
    /// </summary>
    public OverreadEvaluationLookupConfig OverreadEvaluationLookup { get; set; } = new();

    public class OverreadEvaluationLookupConfig
    {
        /// <summary>
        /// Number of seconds to delay each retry of looking up the evaluation associated with
        /// an overread
        /// </summary>
        public int DelayedRetrySeconds { get; set; }

        /// <summary>
        /// Limit for how long to continue retrying the looking up the evaluation associated
        /// with an overread
        /// </summary>
        public int RetryLimitSeconds { get; set; }
    }
}