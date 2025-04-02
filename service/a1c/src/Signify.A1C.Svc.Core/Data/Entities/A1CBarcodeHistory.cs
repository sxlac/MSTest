using System;

namespace Signify.A1C.Svc.Core.Data.Entities
{
    public sealed class A1CBarcodeHistory
    {
        public int A1CBarcodeHistoryId { get; set; }
        public string Barcode { get; set; }
        public int A1CId { get; set; }
        //Foreign key
        public A1C A1C { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public A1CBarcodeHistory()
        {

        }

        public A1CBarcodeHistory(int id, string barcode, int a1CId, DateTimeOffset createdDateTime)
        {
            A1CBarcodeHistoryId = id;
            Barcode = barcode;
            A1CId = a1CId;
            CreatedDateTime = createdDateTime;
        }

        public A1CBarcodeHistory(int id, string barcode, int a1CId, A1C a1c, DateTimeOffset createdDateTime)
        {
            A1CBarcodeHistoryId = id;
            Barcode = barcode;
            A1C = a1c;
            A1CId = a1CId;
            CreatedDateTime = createdDateTime;
        }

        private bool Equals(A1CBarcodeHistory other)
        {
            return A1CBarcodeHistoryId == other.A1CBarcodeHistoryId && Equals(Barcode, other.Barcode) && A1CId == other.A1CId && Equals(A1C, other.A1C) && CreatedDateTime.Equals(other.CreatedDateTime);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((A1CBarcodeHistory)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = A1CBarcodeHistoryId;
                hashCode = (hashCode * 397) ^ (Barcode != null ? Barcode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ A1CId.GetHashCode();
                hashCode = (hashCode * 397) ^ (A1C != null ? A1C.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(A1CBarcodeHistoryId)}: {A1CBarcodeHistoryId}, {nameof(Barcode)}: {Barcode}, {nameof(A1CId)}:{A1CId}, {nameof(A1C)}: {A1C}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
        }
    }
}
