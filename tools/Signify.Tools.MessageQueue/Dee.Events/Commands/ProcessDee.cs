using NServiceBus;

namespace Signify.DEE.Svc.Core.Commands
{
    public class ProcessDee : ICommand
    {
        public int EvaluationId { get; set; }
    }

    public class ProcessDeeInternal
    {
        public DateTime DateOfService { get; set; }
        public int ExamId { get; set; }
        public int DeeExamId { get; set; }
        public long MemberPlanId { get; set; }
        public int EvaluationId { get; set; }
    }
}
