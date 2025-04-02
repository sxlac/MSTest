using System;

namespace Signify.A1C.Svc.Core.ApiClient.Requests
{
    public class CreateOrder 
    {
        public DateTime? DOB { get; set; }
        public int? MemberPlanId { get; set; }
        public string ClientId { get; set; }
        public string AppointmentId { get; set; }
        public int? PlanId { get; set; }
        public string SubscriberId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public char Gender { get; set; }
        // other name is - labtesttype
        public string SampleType { get; set; }
        public DateTime? DateOfService { get; set; }
        public int? EvaluationId { get; set; }
        public string CenseoId { get; set; }
        public string ProviderName { get; set; }
        public string Vendor { get; set; }
        public string MemberFlag { get; set; }
        public string SampleId { get; set; }
        public string HomePhone { get; set; }
        public Guid? OrderCorrelationId { get; set; }
    }
}
