using NServiceBus;
using System;

namespace Signify.FOBT.Messages.Events;
/*********************** Phase 2 implementation ***********************/

public class LabResultsReceived : IEvent
{
	public int FOBTId { get; set; }

	public int MemberPlanId { get; set; }

	public int MemberId { get; set; }

	public int AppointmentId { get; set; }

	public int ProviderId { get; set; }

	public DateTime? DateOfService { get; set; }

	public DateTimeOffset CreatedDateTime { get; set; }

	public DateTime ReceivedDateTime { get; set; }

	public string Barcode { get; set; }

	public int ClientId { get; set; }

	public string UserName { get; set; }

	public string ApplicationId { get; set; }

}