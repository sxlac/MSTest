using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public class ResultsModel
{
    /// <summary>
    /// Raw string value of the result from the evaluation service
    /// </summary>
    public string RawValue { get; set; }

    /// <summary>
    /// Parsed value of the result from the evaluation service
    /// </summary>
    /// <remarks>
    /// May be null if the raw value is invalid
    /// </remarks>
    public decimal? ParsedValue { get; set; }

    public ResultValueRange ValueRange { get; set; }

    public Normality Normality { get; set; }

    /// <summary>
    /// Human-readable exception as to why the <see cref="RawValue"/> is not valid and the normality is Undetermined
    /// </summary>
    public string Exception { get; set; }
}