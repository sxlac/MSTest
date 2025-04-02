using System;
using System.Diagnostics.CodeAnalysis;
using NServiceBus;

namespace Signify.FOBT.Svc.Core.Sagas;

[ExcludeFromCodeCoverage]
public sealed class InvUpdateReceived : IEvent
{
    public Guid RequestId { get; set; }
    public string ItemNumber { get; set; }
    public Result Result { get; set; }
    public string SerialNumber { get; set; }
    public int Quantity { get; set; }
    public int ProviderId { get; set; }
    public DateTime DateUpdated { get; set; }

    public InvUpdateReceived(Guid requestId, string itemNumber, Result result, string serialNumber, int quantity, int providerId, DateTime dateUpdated)
    {
        RequestId = requestId;
        ItemNumber = itemNumber;
        Result = result;
        SerialNumber = serialNumber;
        Quantity = quantity;
        ProviderId = providerId;
        DateUpdated = dateUpdated;
    }

    public override string ToString()
    {
        return $"{nameof(RequestId)}: {RequestId}, {nameof(ItemNumber)}: {ItemNumber}, {nameof(Result)}: {Result}, {nameof(SerialNumber)}: {SerialNumber}, {nameof(Quantity)}: {Quantity}, {nameof(ProviderId)}: {ProviderId}, {nameof(DateUpdated)}: {DateUpdated}";
    }

    private bool Equals(InvUpdateReceived other)
    {
        return RequestId.Equals(other.RequestId) && ItemNumber == other.ItemNumber && Equals(Result, other.Result) && SerialNumber == other.SerialNumber && Quantity == other.Quantity && ProviderId == other.ProviderId && DateUpdated.Equals(other.DateUpdated);
    }

    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj) || obj is InvUpdateReceived other && Equals(other);
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
}