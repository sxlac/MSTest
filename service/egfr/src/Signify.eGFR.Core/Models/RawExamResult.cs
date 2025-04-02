using System;

namespace Signify.eGFR.Core.Models;

/// <summary>
/// Results of a eGFR test that was performed, but with some result values not of
/// their actual data types or validated against valid values. Instead, contains the raw
/// answer values from the Evaluation API.
/// </summary>
public sealed class RawExamResult : IEquatable<RawExamResult>
{
    /// <summary>
    /// Identifier of the evaluation that contained this eGFR exam
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
        return ReferenceEquals(this, obj) || obj is RawExamResult other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EvaluationId, Barcode);
    }

    public static bool operator ==(RawExamResult left, RawExamResult right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RawExamResult left, RawExamResult right)
    {
        return !Equals(left, right);
    }
    #endregion IEquatable
}