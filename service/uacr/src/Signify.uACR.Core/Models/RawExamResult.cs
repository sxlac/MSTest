using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Models;

/// <summary>
/// Results of a uACR test that was performed, but with some result values not of
/// their actual data types or validated against valid values. Instead, contains the raw
/// answer values from the Evaluation API.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RawExamResult : IEquatable<RawExamResult>
{
    /// <summary>
    /// Identifier of the evaluation that contained this uACR exam
    /// </summary>
    public long EvaluationId { get; set; }

    /// <summary>
    /// Barcode provided with evaluation
    /// </summary>
    /// <remarks>
    /// This is the raw, unvalidated <see cref="string"/> answer value from the Evaluation API
    /// </remarks>
    public string Barcode { get; set; }

    /// <summary>
    /// Tells whether a barcode is the correct format
    /// </summary>
    public bool ValidBarcode { get; set; }

    #region IEquatable
    public bool Equals(RawExamResult other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EvaluationId == other.EvaluationId
               && Barcode == other.Barcode;                  
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType()
               && Equals((RawExamResult) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = EvaluationId;
            hashCode = (hashCode * 397) ^ (Barcode != null ? Barcode.GetHashCode() : 0);               
            return hashCode.GetHashCode();
        }
    }
    #endregion IEquatable
}