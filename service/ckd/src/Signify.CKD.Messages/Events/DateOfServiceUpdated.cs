using NServiceBus;
using System;

namespace Signify.CKD.Messages.Events
{
	public sealed class DateOfServiceUpdated : IMessage
	{
		public long EvaluationId { get; set; }
		public DateTime DateOfService { get; set; }

		public DateOfServiceUpdated()
		{
		}

		public DateOfServiceUpdated(long evaluationId, DateTime dateOfService)
		{
			EvaluationId = evaluationId;
			DateOfService = dateOfService;
		}
	}
}
