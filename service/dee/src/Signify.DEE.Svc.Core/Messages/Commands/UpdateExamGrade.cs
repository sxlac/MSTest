using MediatR;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class UpdateExamGrade : IRequest<Unit>
{
    public int ExamId { get; set; }
    public bool Gradable { get; set; }
}

public class UpdateExamGradeHandler(DataContext context) : IRequestHandler<UpdateExamGrade, Unit>
{
    [Trace]
    public async Task<Unit> Handle(UpdateExamGrade request, CancellationToken cancellationToken)
    {
        var exam = context.Exams.FirstOrDefault(ex => ex.ExamId == request.ExamId);
        if (exam != null)
        {
            exam.Gradeable = request.Gradable;
            await context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}