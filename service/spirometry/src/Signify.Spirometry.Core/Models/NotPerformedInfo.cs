using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Models;

[ExcludeFromCodeCoverage]
public sealed class NotPerformedInfo : IEquatable<NotPerformedInfo>
{
    /// <summary>
    /// Reason why the provider did not perform the spirometry exam
    /// </summary>
    public NotPerformedReason Reason { get; }

    /// <summary>
    /// Optional free-text notes as to why the provider did not perform the spirometry exam
    /// </summary>
    public string Notes { get; }

    public NotPerformedInfo(NotPerformedReason reason, string notes = null)
    {
        Reason = reason;
        Notes = notes;
    }

    #region IEquatable
    public bool Equals(NotPerformedInfo other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Reason == other.Reason && Notes == other.Notes;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((NotPerformedInfo) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int) Reason, Notes);
    }

    public static bool operator ==(NotPerformedInfo left, NotPerformedInfo right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(NotPerformedInfo left, NotPerformedInfo right)
    {
        return !Equals(left, right);
    }
    #endregion IEquatable
}