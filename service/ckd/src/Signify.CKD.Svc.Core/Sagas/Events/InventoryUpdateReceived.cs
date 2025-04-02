using System;
using NServiceBus;

namespace Signify.CKD.Sagas
{
	public class InventoryUpdateReceived : IMessage
	{
		public Guid RequestId { get; set; }
		public string ItemNumber { get; set; }
		public Result Result { get; set; }
		public int Quantity { get; set; }
		public int ProviderId { get; set; }
		public DateTime DateUpdated { get; set; }
		public DateTime? ExpirationDate { get; set; }


		public InventoryUpdateReceived(Guid requestId, string itemNumber, Result result, int quantity, int providerId, DateTime dateUpdated, DateTime? expirationDate)
		{
			RequestId = requestId;
			ItemNumber = itemNumber;
			Result = result;
			Quantity = quantity;
			ProviderId = providerId;
			DateUpdated = dateUpdated;
			ExpirationDate = expirationDate;
		}
	}

	public class Result
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
	}
}
