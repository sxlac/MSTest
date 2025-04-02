using System;
using System.Collections.Generic;
using Signify.A1C.Svc.Core.Events;

namespace Signify.A1C.Svc.Core.ApiClient.Response
{
	public class EvaluationVersionRs
	{
		public int Version { get; set; }
		public EvaluationModel Evaluation { get; set; }
	}

	public class EvaluationModel
	{
		public int Id { get; set; }
		public int ClientId { get; set; }
		public int ProviderId { get; set; }
		public List<EvaluationAnswerModel> Answers { get; set; }
        public List<Product> Products { get; set; }
        public DateTime? DateOfService { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public DateTime ReceivedDateTime { get; set; }
        public int EvaluationTypeId { get; set; }
        public int FormVersionId { get; set; }
        public string UserName { get; set; }
        public int AppointmentId { get; set; }
        public Location Location { get; set; }
	}

	public class EvaluationAnswerModel
	{

		public string FormAnswerMeaningId { get; set; }
		public int AnswerId { get; set; }
		public int QuestionId { get; set; }
		public string AnswerValue { get; set; }
		public Guid? AnswerRowId { get; set; }
		public int Id { get; set; }
	}
}