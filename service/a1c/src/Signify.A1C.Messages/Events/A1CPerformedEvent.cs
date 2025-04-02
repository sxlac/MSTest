using System;
using NServiceBus;

namespace Signify.A1C.Messages.Events
{
    public sealed class A1CPerformedEvent : IEvent
    {
        public Guid CorrelationId { get; set; }

        public int A1CId { get; set; }

        public int EvaluationId { get; set; }

        public int MemberPlanId { get; set; }

        public int MemberId { get; set; }
        public string PlanId { get; set; }

        public int? AppointmentId { get; set; }

        public int ProviderId { get; set; }

        public DateTime? DateOfService { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public DateTime ReceivedDateTime { get; set; }

        public string Barcode { get; set; }

        public int? ClientId { get; set; }

        public string UserName { get; set; }

        public string ApplicationId { get; set; }
        public string ProviderName { get; set; }
        public string Gender { get; set; }
        public string SubscriberId { get; set; }
        public string HomePhone { get; set; }

        private bool Equals(A1CPerformedEvent other)
        {
            return CorrelationId.Equals(other.CorrelationId) && A1CId == other.A1CId && EvaluationId == other.EvaluationId && MemberPlanId == other.MemberPlanId &&
                   MemberId == other.MemberId && AppointmentId == other.AppointmentId &&
                   ProviderId == other.ProviderId && Nullable.Equals(DateOfService, other.DateOfService) &&
                   CreatedDateTime.Equals(other.CreatedDateTime) && ReceivedDateTime.Equals(other.ReceivedDateTime) &&
                   Barcode == other.Barcode && ClientId == other.ClientId && UserName == other.UserName &&
                   ApplicationId == other.ApplicationId && ProviderName == other.ProviderName && Gender == other.Gender && PlanId == other.PlanId && SubscriberId == other.SubscriberId && HomePhone == other.HomePhone ;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((A1CPerformedEvent)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CorrelationId.GetHashCode();
                hashCode = (hashCode * 397) ^ A1CId;
                hashCode = (hashCode * 397) ^ EvaluationId;
                hashCode = (hashCode * 397) ^ MemberPlanId;
                hashCode = (hashCode * 397) ^ MemberId;
                hashCode = (hashCode * 397) ^ (AppointmentId!=null?AppointmentId.GetHashCode():0);
                hashCode = (hashCode * 397) ^ ProviderId;
                hashCode = (hashCode * 397) ^ DateOfService.GetHashCode();
                hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ ReceivedDateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ (Barcode != null ? Barcode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ClientId != null ? ClientId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (UserName != null ? UserName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ApplicationId != null ? ApplicationId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProviderName != null ? ProviderName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Gender != null ? Gender.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PlanId != null ? PlanId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SubscriberId != null ? SubscriberId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (HomePhone != null ? HomePhone.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return
                $"{nameof(CorrelationId)}: {CorrelationId}, {nameof(A1CId)}: {A1CId}, {nameof(EvaluationId)}: {EvaluationId}, {nameof(MemberPlanId)}: {MemberPlanId}, {nameof(MemberId)}: {MemberId}, {nameof(AppointmentId)}: {AppointmentId}, {nameof(ProviderId)}: {ProviderId}, {nameof(DateOfService)}: {DateOfService}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(ReceivedDateTime)}: {ReceivedDateTime}, {nameof(Barcode)}: {Barcode}, {nameof(ClientId)}: {ClientId}, {nameof(UserName)}: {UserName}, {nameof(ApplicationId)}: {ApplicationId}, {nameof(ProviderName)}: {ProviderName}, {nameof(Gender)}: {Gender}, {nameof(HomePhone)}: {PlanId}, {nameof(SubscriberId)}: {SubscriberId}, {nameof(HomePhone)}: {HomePhone}";
        }
    }
}