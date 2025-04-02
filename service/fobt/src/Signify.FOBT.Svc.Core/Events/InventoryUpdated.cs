using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Events;

[ExcludeFromCodeCoverage]
public sealed class InventoryUpdated
{
    public Guid RequestId { get; set; }
    public string ItemNumber { get; set; }
    public Result Result { get; set; }
    public string SerialNumber { get; set; }
    public int Quantity { get; set; }
    public int ProviderId { get; set; }
    public DateTime DateUpdated { get; set; }

    public InventoryUpdated(Guid requestId, string itemNumber, Result result, string serialNumber, int quantity, int providerId, DateTime dateUpdated, DateTime? expirationDate)
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

    private bool Equals(InventoryUpdated other)
    {
        return RequestId.Equals(other.RequestId) && ItemNumber == other.ItemNumber && Equals(Result, other.Result) && SerialNumber == other.SerialNumber && Quantity == other.Quantity && ProviderId == other.ProviderId && DateUpdated.Equals(other.DateUpdated);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((InventoryUpdated)obj);
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

    public static InventoryUpdated Create(Guid requestId, string itemNumber, Result result, string serialNumber, int quantity, int providerId, DateTime dateUpdated, DateTime? expirationDate)
        => new InventoryUpdated(requestId, itemNumber, result, serialNumber, quantity, providerId, dateUpdated, expirationDate);
}

[ExcludeFromCodeCoverage]
public sealed class Result
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }

    public Result()
    {
    }

    public Result(bool isSuccess, string errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    private bool Equals(Result other)
    {
        return IsSuccess == other.IsSuccess && ErrorMessage == other.ErrorMessage;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Result)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (IsSuccess.GetHashCode() * 397) ^ (ErrorMessage != null ? ErrorMessage.GetHashCode() : 0);
        }
    }

    public static Result Create(bool isSuccess = true, string errorMessage = "")
        => new Result(isSuccess, errorMessage);
}