using System;

namespace Signify.CKD.Messages.Events
{
	public sealed class InventoryUpdated
	{
		public Guid RequestId { get; set; }
		public string ItemNumber { get; set; }
		public Result Result { get; set; }
		public string SerialNumber { get; set; }
		public int Quantity { get; set; }
		public int ProviderId { get; set; }
		public DateTime DateUpdated { get; set; }
		public DateTime? ExpirationDate { get; set; }

		public override string ToString()
		{
			return $"{nameof(RequestId)}: {RequestId}, {nameof(ItemNumber)}: {ItemNumber}, {nameof(Result)}: {Result}, {nameof(SerialNumber)}: {SerialNumber}, {nameof(Quantity)}: {Quantity}, {nameof(ProviderId)}: {ProviderId}, {nameof(DateUpdated)}: {DateUpdated}, {nameof(ExpirationDate)}: {ExpirationDate}";
		}

		private bool Equals(InventoryUpdated other)
		{
			return RequestId.Equals(other.RequestId) && ItemNumber == other.ItemNumber && Equals(Result, other.Result) && SerialNumber == other.SerialNumber && Quantity == other.Quantity && ProviderId == other.ProviderId && DateUpdated.Equals(other.DateUpdated) && Nullable.Equals(ExpirationDate, other.ExpirationDate);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((InventoryUpdated) obj);
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
				hashCode = (hashCode * 397) ^ ExpirationDate.GetHashCode();
				return hashCode;
			}
		}
	}



	public sealed class Result
	{
		public bool IsSuccess { get; set; }
		public string ErrorMessage { get; set; }

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
	}
}
