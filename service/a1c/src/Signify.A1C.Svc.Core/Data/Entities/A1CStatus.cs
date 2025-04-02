using System;

namespace Signify.A1C.Svc.Core.Data.Entities
{
    public class A1CStatus
    {
        public int A1CStatusId { get; set; }

        //Foreign key 
        public A1CStatusCode A1CStatusCode { get; set; }

        public int A1CId { get; set; }

        //Foreign key
        public A1C A1C { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public A1CStatus()
        {

        }

        public A1CStatus(int a1CStatusId, A1CStatusCode a1CStatusCode, DateTimeOffset createdDateTime, int a1CId)
        {
            A1CStatusId = a1CStatusId;
            A1CStatusCode = a1CStatusCode;
            CreatedDateTime = createdDateTime;
            A1CId = a1CId;
        }

        public A1CStatus(int a1CStatusId, A1CStatusCode a1CStatusCode, A1C a1C, DateTimeOffset createdDateTime, int a1CId)
        {
            A1CStatusId = a1CStatusId;
            A1CStatusCode = a1CStatusCode;
            A1C = a1C;
            CreatedDateTime = createdDateTime;
            A1CId = a1CId;
        }

        private bool Equals(A1CStatus other)
        {
            return A1CStatusId == other.A1CStatusId && Equals(A1CStatusCode, other.A1CStatusCode) && A1CId == other.A1CId && Equals(A1C, other.A1C) && CreatedDateTime.Equals(other.CreatedDateTime);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((A1CStatus)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = A1CStatusId;
                hashCode = (hashCode * 397) ^ (A1CStatusCode != null ? A1CStatusCode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ A1CId.GetHashCode();
                hashCode = (hashCode * 397) ^ (A1C != null ? A1C.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(A1CStatusId)}: {A1CStatusId}, {nameof(A1CStatusCode)}: {A1CStatusCode}, {nameof(A1CId)}: {A1CId},{nameof(A1C)}: {A1C}, {nameof(CreatedDateTime)}: {CreatedDateTime}";
        }
    }
}