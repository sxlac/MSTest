using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class WaveformDocumentVendor : IEquatable<WaveformDocumentVendor>
{
    [Key]
    public int WaveformDocumentVendorId { get; set; }
    public string VendorName { get; set; }

    public WaveformDocumentVendor()
    {
    }

    public WaveformDocumentVendor(int vendorId, string vendorName)
    {
        WaveformDocumentVendorId = vendorId;
        VendorName = vendorName;
    }

    public override string ToString()
        => $"{nameof(WaveformDocumentVendorId)}: {WaveformDocumentVendorId}, {nameof(VendorName)}: {VendorName}";

    #region IEquatable
    public bool Equals(WaveformDocumentVendor other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return WaveformDocumentVendorId == other.WaveformDocumentVendorId && VendorName == other.VendorName;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((WaveformDocumentVendor)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = WaveformDocumentVendorId;
            hashCode = (hashCode * 397) ^ (VendorName != null ? VendorName.GetHashCode() : 0);
            return hashCode;
        }
    }

    public static bool operator ==(WaveformDocumentVendor left, WaveformDocumentVendor right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(WaveformDocumentVendor left, WaveformDocumentVendor right)
    {
        return !Equals(left, right);
    }
    #endregion IEquatable
}