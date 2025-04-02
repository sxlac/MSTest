using System;
using NServiceBus;

namespace Signify.CKD.Messages.Events
{
	public sealed class CKDPerformed : IMessage
	{
		public Guid CorrelationId { get; set; }

		public int CKDId { get; set; }

		public int EvaluationId { get; set; }

		public int MemberPlanId { get; set; }

		public int MemberId { get; set; }

		public int AppointmentId { get; set; }

		public int ProviderId { get; set; }

		public DateTime? DateOfService { get; set; }

		public DateTimeOffset CreatedDateTime { get; set; }

		public DateTime ReceivedDateTime { get; set; }

		public int ClientId { get; set; }

		public string UserName { get; set; }

		public string ApplicationId { get; set; }
		
		public DateTime? ExpirationDate { get; set; }
	}
}
