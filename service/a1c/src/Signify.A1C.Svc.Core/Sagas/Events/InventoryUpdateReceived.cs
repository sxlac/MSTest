using System;
using NServiceBus;

namespace Signify.A1C.Svc.Core.Sagas
{
    public sealed class InventoryUpdateReceived : IEvent
    {
        public Guid RequestId { get; set; }
        public string ItemNumber { get; set; }
        public Result Result { get; set; }
        public string SerialNumber { get; set; }
        public int Quantity { get; set; }
        public int ProviderId { get; set; }
        public DateTime DateUpdated { get; set; }

        public InventoryUpdateReceived(Guid requestId, string itemNumber, Result result, string serialNumber, int quantity, int providerId, DateTime dateUpdated)
        {
            RequestId = requestId;
            ItemNumber = itemNumber;
            Result = result;
            SerialNumber = serialNumber;
            Quantity = quantity;
            ProviderId = providerId;
            DateUpdated = dateUpdated;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = RequestId.GetHashCode();
                hashCode = (hashCode * 397) ^ (ItemNumber != null ? ItemNumber.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Result != null ? Result.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SerialNumber != null ? SerialNumber.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Quantity;
                hashCode = (hashCode * 397) ^ ProviderId;
                hashCode = (hashCode * 397) ^ DateUpdated.GetHashCode();
                return hashCode;
            }
        }
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((InventoryUpdateReceived)obj);
        }

        private bool Equals(InventoryUpdateReceived other)
        {
            return RequestId.Equals(other.RequestId) && ItemNumber == other.ItemNumber && Equals(Result, other.Result) && SerialNumber == other.SerialNumber && Quantity == other.Quantity && ProviderId == other.ProviderId && DateUpdated.Equals(other.DateUpdated);
        }

        public bool Equals(InventoryUpdateReceived x, InventoryUpdateReceived y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.RequestId.Equals(y.RequestId) && x.ItemNumber == y.ItemNumber && Equals(x.Result, y.Result) && x.SerialNumber == y.SerialNumber && x.Quantity == y.Quantity && x.ProviderId == y.ProviderId && x.DateUpdated.Equals(y.DateUpdated);
        }

        public override string ToString()
        {
            return $"{nameof(RequestId)}: {RequestId}, {nameof(ItemNumber)}: {ItemNumber}, {nameof(Result)}: {Result}, {nameof(SerialNumber)}:{SerialNumber}, {nameof(Quantity)}: {Quantity}, {nameof(ProviderId)}: {ProviderId}, {nameof(DateUpdated)}: {DateUpdated}";
        }
    }
}
