using System;

namespace Signify.A1C.Svc.Core.Events
{
	/************ Phase 2 implementation ************************/
	public class LabResultsReceivedEvent
	{
		public Guid Id { get; set; }
		public int? ProviderId { get; set; }
		public string UserName { get; set; }
		public int AppointmentId { get; set; }
		public string ApplicationId { get; set; }
		public int MemberPlanId { get; set; }
		public int MemberId { get; set; }
		public int ClientId { get; set; }
		public string DocumentPath { get; set; }
		public DateTimeOffset CreatedDateTime { get; set; }
		public DateTime ReceivedDateTime { get; set; }
		public DateTime? DateOfService { get; set; }
	}
}
