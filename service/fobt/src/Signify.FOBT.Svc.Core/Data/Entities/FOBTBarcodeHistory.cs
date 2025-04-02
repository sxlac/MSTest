using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class FOBTBarcodeHistory
{
    public int FOBTBarcodeHistoryId { get; set; }
    public string Barcode { get; set; }

    //Foreign key
    public virtual FOBT FOBT { get; set; }
    public int FOBTId { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    public Guid? OrderCorrelationId { get; set; }

    public FOBTBarcodeHistory()
    {
    }

    public FOBTBarcodeHistory(int id, string barcode, FOBT fobt, DateTimeOffset createdDateTime, Guid orderCorrelationId)
    {
        FOBTBarcodeHistoryId = id;
        Barcode = barcode;
        FOBT = fobt;
        CreatedDateTime = createdDateTime;
        OrderCorrelationId = orderCorrelationId;
    }

    protected bool Equals(FOBTBarcodeHistory other)
    {
        return FOBTBarcodeHistoryId == other.FOBTBarcodeHistoryId
               && Equals(Barcode, other.Barcode)
               && Equals(FOBT, other.FOBT)
               && CreatedDateTime.Equals(other.CreatedDateTime)
               && OrderCorrelationId.Equals(other.OrderCorrelationId);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((FOBTBarcodeHistory)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = FOBTBarcodeHistoryId;
            hashCode = (hashCode * 397) ^ (Barcode != null ? Barcode.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (FOBT != null ? FOBT.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
            hashCode = (hashCode * 397) ^ OrderCorrelationId.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString()
    {
        return $"{nameof(FOBTBarcodeHistoryId)}: {FOBTBarcodeHistoryId}, {nameof(Barcode)}: {Barcode}, {nameof(FOBT)}: {FOBT}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(OrderCorrelationId)}: {OrderCorrelationId}";
    }
}