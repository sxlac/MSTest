using System;

namespace Signify.CKD.Svc.Core.Data.Entities
{
    public class CKDRCMBilling
    {
        public int Id { get; set; }
        public int CKDId { get; set; }

        public string BillId { get; set; }

        //Foreign key
        public virtual CKD CKD { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        protected bool Equals(CKDRCMBilling other)
        {
            return Id == other.Id && CKDId == other.CKDId && BillId == other.BillId && Equals(CKD, other.CKD) && CreatedDateTime.Equals(other.CreatedDateTime);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CKDRCMBilling)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ BillId.GetHashCode();
                hashCode = (hashCode * 397) ^ CKDId.GetHashCode();
                hashCode = (hashCode * 397) ^ (CKD != null ? CKD.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(BillId)}: {BillId}, {nameof(CKD)}: {CKD}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
        }
    }
}
