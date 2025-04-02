using NServiceBus;

namespace Signify.DEE.Svc.Core.Commands
{
	public class ProcessIrisResultPdf : ICommand
	{
		public long EvaluationId { get; set; }
		public DateTime CreatedDateTime { get; set; }
		public string PdfData { get; set; }
		//Below will be called exam local Id once we fully integrate with IRIS SB
		public int DeeExamId { get; set; }
		//Could be that localId and examId is the same.
		public int ExamId { get; set; }

		public ProcessIrisResultPdf(int evaluationId, DateTime createdDateTime, string pdfData, int deeExamId, int examId)
		{
			EvaluationId = evaluationId;
			CreatedDateTime = createdDateTime;
			PdfData = pdfData;
			DeeExamId = deeExamId;
			ExamId = examId;
		}

		public ProcessIrisResultPdf()
		{

		}

    }

}
