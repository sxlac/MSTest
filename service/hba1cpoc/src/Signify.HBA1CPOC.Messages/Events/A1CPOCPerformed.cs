using System;
using NServiceBus;

namespace Signify.HBA1CPOC.Messages.Events
{
    public sealed class A1CPOCPerformed : IEvent
	{ 
        public Guid CorrelationId { get; set; }

		public int HBA1CPOCId { get; set; }

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
		
		public DateOnly? ExpirationDate { get; set; }
		
		private bool Equals(A1CPOCPerformed other)
		{
			return CorrelationId.Equals(other.CorrelationId) && HBA1CPOCId == other.HBA1CPOCId && EvaluationId == other.EvaluationId && MemberPlanId == other.MemberPlanId && MemberId == other.MemberId && AppointmentId == other.AppointmentId && ProviderId == other.ProviderId && Nullable.Equals(DateOfService, other.DateOfService) && CreatedDateTime.Equals(other.CreatedDateTime) && ReceivedDateTime.Equals(other.ReceivedDateTime) && ClientId == other.ClientId && UserName == other.UserName && ApplicationId == other.ApplicationId && Nullable.Equals(ExpirationDate, other.ExpirationDate);
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == this.GetType() && Equals((A1CPOCPerformed) obj);
        }

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = CorrelationId.GetHashCode();
				hashCode = (hashCode * 397) ^ HBA1CPOCId;
				hashCode = (hashCode * 397) ^ EvaluationId;
				hashCode = (hashCode * 397) ^ MemberPlanId;
				hashCode = (hashCode * 397) ^ MemberId;
				hashCode = (hashCode * 397) ^ AppointmentId;
				hashCode = (hashCode * 397) ^ ProviderId;
				hashCode = (hashCode * 397) ^ DateOfService.GetHashCode();
				hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
				hashCode = (hashCode * 397) ^ ReceivedDateTime.GetHashCode();
				hashCode = (hashCode * 397) ^ ClientId;
				hashCode = (hashCode * 397) ^ (UserName != null ? UserName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (ApplicationId != null ? ApplicationId.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ ExpirationDate.GetHashCode();
				return hashCode;
			}
		}
	}
}
