using NServiceBus;

namespace Signify.DEE.Svc.Core.Commands
{
    public class CreateDee : ICommand
    {
        public Guid Id { get; set; }
        public int EvaluationId { get; set; }
        public int EvaluationTypeId { get; set; }
        public int FormVersionId { get; set; }
        public int? ProviderId { get; set; }
        public string UserName { get; set; }
        public int AppointmentId { get; set; }
        public string ApplicationId { get; set; }
        public int MemberId { get; set; }
        public int ClientId { get; set; }
        public string DocumentPath { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public DateTime ReceivedDateTime { get; set; }

        public CreateDee(Guid id, int evaluationId, int evaluationTypeId, int formVersionId, int? providerId, string userName, int appointmentId, string applicationId, int memberId, int clientId, string documentPath, DateTimeOffset createdDateTime, DateTime receivedDateTime)
        {
            Id = id;
            EvaluationId = evaluationId;
            EvaluationTypeId = evaluationTypeId;
            FormVersionId = formVersionId;
            ProviderId = providerId;
            UserName = userName;
            AppointmentId = appointmentId;
            ApplicationId = applicationId;
            MemberId = memberId;
            ClientId = clientId;
            DocumentPath = documentPath;
            CreatedDateTime = createdDateTime;
            ReceivedDateTime = receivedDateTime;
        }

        public CreateDee()
        {

        }

        protected bool Equals(CreateDee other)
        {
            return Id.Equals(other.Id) && EvaluationId == other.EvaluationId && EvaluationTypeId == other.EvaluationTypeId && FormVersionId == other.FormVersionId && ProviderId == other.ProviderId && UserName == other.UserName && AppointmentId == other.AppointmentId && ApplicationId == other.ApplicationId && MemberId == other.MemberId && ClientId == other.ClientId && DocumentPath == other.DocumentPath && CreatedDateTime.Equals(other.CreatedDateTime) && ReceivedDateTime.Equals(other.ReceivedDateTime);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CreateDee)obj);
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
                hashCode = (hashCode * 397) ^ MemberId;
                hashCode = (hashCode * 397) ^ ClientId;
                hashCode = (hashCode * 397) ^ (DocumentPath != null ? DocumentPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ ReceivedDateTime.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(EvaluationId)}: {EvaluationId}, {nameof(EvaluationTypeId)}: {EvaluationTypeId}, {nameof(FormVersionId)}: {FormVersionId}, {nameof(ProviderId)}: {ProviderId}, {nameof(UserName)}: {UserName}, {nameof(AppointmentId)}: {AppointmentId}, {nameof(ApplicationId)}: {ApplicationId}, {nameof(MemberId)}: {MemberId}, {nameof(ClientId)}: {ClientId}, {nameof(DocumentPath)}: {DocumentPath}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(ReceivedDateTime)}: {ReceivedDateTime}";
        }
    }
}