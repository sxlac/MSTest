using System;
using NServiceBus;

namespace Signify.HBA1CPOC.Messages.Events
{
    public sealed class EvalReceived : IEvent
	{ 
        public Guid Id { get; set; }
		public int EvaluationId { get; set; }
		public int EvaluationTypeId { get; set; }
		public int FormVersionId { get; set; }
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
		public bool IsLabPerformed { get; set; }

		private bool Equals(EvalReceived other)
		{
			return Id.Equals(other.Id)
				&& EvaluationId == other.EvaluationId
				&& EvaluationTypeId == other.EvaluationTypeId
				&& FormVersionId == other.FormVersionId
				&& ProviderId == other.ProviderId
				&& UserName == other.UserName
				&& AppointmentId == other.AppointmentId
				&& ApplicationId == other.ApplicationId
				&& MemberPlanId == other.MemberPlanId
				&& MemberId == other.MemberId
				&& ClientId == other.ClientId
				&& DocumentPath == other.DocumentPath
				&& CreatedDateTime.Equals(other.CreatedDateTime)
				&& ReceivedDateTime.Equals(other.ReceivedDateTime)
				&& Nullable.Equals(DateOfService, other.DateOfService)
				&& IsLabPerformed == other.IsLabPerformed;
		}

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == this.GetType() && Equals((EvalReceived) obj);
        }

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Id.GetHashCode();
				hashCode = (hashCode * 397) ^ EvaluationId;
				hashCode = (hashCode * 397) ^ EvaluationTypeId;
				hashCode = (hashCode * 397) ^ FormVersionId;
				hashCode = (hashCode * 397) ^ ProviderId.GetHashCode();
				hashCode = (hashCode * 397) ^ (UserName != null ? UserName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ AppointmentId;
				hashCode = (hashCode * 397) ^ (ApplicationId != null ? ApplicationId.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ MemberPlanId;
				hashCode = (hashCode * 397) ^ MemberId;
				hashCode = (hashCode * 397) ^ ClientId;
				hashCode = (hashCode * 397) ^ (DocumentPath != null ? DocumentPath.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
				hashCode = (hashCode * 397) ^ ReceivedDateTime.GetHashCode();
				hashCode = (hashCode * 397) ^ DateOfService.GetHashCode();
				hashCode = (hashCode * 397) ^ IsLabPerformed.GetHashCode();
				return hashCode;
			}
		}

		public override string ToString()
		{
			return $"{nameof(Id)}: {Id}, {nameof(EvaluationId)}: {EvaluationId}, {nameof(EvaluationTypeId)}: {EvaluationTypeId}, {nameof(FormVersionId)}: {FormVersionId}, {nameof(ProviderId)}: {ProviderId}, {nameof(UserName)}: {UserName}, {nameof(AppointmentId)}: {AppointmentId}, {nameof(ApplicationId)}: {ApplicationId}, {nameof(MemberPlanId)}: {MemberPlanId}, {nameof(MemberId)}: {MemberId}, {nameof(ClientId)}: {ClientId}, {nameof(DocumentPath)}: {DocumentPath}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(ReceivedDateTime)}: {ReceivedDateTime}, {nameof(DateOfService)}: {DateOfService}, {nameof(IsLabPerformed)}: {IsLabPerformed}";
		}
	}
}
