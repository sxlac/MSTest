namespace Signify.HBA1CPOC.Svc.Core.Models;

/// <remarks>
/// Unfortunately we have to have this convoluted logic here because the raw string value may be a decimal,
/// or it may be "&lt;4" or "&gt;13"...
/// </remarks>
public enum ResultValueRange
{
    /// <summary>
    /// The <see cref="ResultsModel.ParsedValue"/> is the exact result
    /// </summary>
    Exactly,
    /// <summary>
    /// The <see cref="ResultsModel.ParsedValue"/> is not the exact result; the result is some value less than
    /// this amount
    /// </summary>
    LessThan,
    /// <summary>
    /// The <see cref="ResultsModel.ParsedValue"/> is not the exact result; the result is some value greater than
    /// this amount
    /// </summary>
    GreaterThan
}