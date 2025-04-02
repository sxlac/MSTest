using System;
using NServiceBus;

namespace Signify.FOBT.Messages.Events
{
    public class CreateOrderEvent : IMessage
    {
        public Guid CorrelationId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public long MemberPlanId { get; set; }
        public int? AppointmentId { get; set; }
        public string PlanId { get; set; }
        public int? ClientId { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string SubscriberId { get; set; }
        public string ProgrameName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string AddressLineOne { get; set; }
        public string AddressLineTwo { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public char? Sex { get; set; }
        public string HomePhone { get; set; }
        public string LabTestType { get; set; }
        public string Barcode { get; set; }
        public string CenseoId { get; set; }
        public string EvaluationId { get; set; }
        public DateTime? DateOfService { get; set; }
        public string ProviderName { get; set; }
    }
}
