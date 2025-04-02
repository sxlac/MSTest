using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Models;

[ExcludeFromCodeCoverage]
public sealed class NotPerformedInfo(NotPerformedReason reason, string notes = null) : IEquatable<NotPerformedInfo>
{
    /// <summary>
    /// Reason why the provider did not perform the uACR exam
    /// </summary>
    public NotPerformedReason Reason { get; } = reason;

    /// <summary>
    /// Optional free-text notes as to why the provider did not perform the uACR exam
    /// </summary>
    public string Notes { get; } = notes;

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