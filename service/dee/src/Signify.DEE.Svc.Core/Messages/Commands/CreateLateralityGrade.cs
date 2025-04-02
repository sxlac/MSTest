using System.Threading;
using System.Threading.Tasks;
using Signify.DEE.Svc.Core.Messages.Models;
using MediatR;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Data;
using NewRelic.Api.Agent;
using Iris.Public.Types.Models;
using Signify.DEE.Svc.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Exceptions;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class CreateLateralityGrade : IRequest<int>
{
    public ExamModel ExamModel { get; set; }
    public ResultEyeSideGrading Grading { get; set; }
    public string LateralityCode { get; set; }
}

public class CreateLateralityGradeHandler(ILogger<CreateLateralityGradeHandler> logger, DataContext context)
    : IRequestHandler<CreateLateralityGrade, int>
{
    [Trace]
    public async Task<int> Handle(CreateLateralityGrade request, CancellationToken cancellationToken)
    {
        var lateralityCodeId = request.LateralityCode switch
        {
            ApplicationConstants.Laterality.RightEyeCode => LateralityCode.Right.LateralityCodeId,
            ApplicationConstants.Laterality.LeftEyeCode => LateralityCode.Left.LateralityCodeId,
            _ => throw new GradeLateralityException(request.ExamModel.EvaluationId)
        };

        var examLateralityGrade = await context.ExamLateralityGrade
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ExamId == request.ExamModel.ExamId
                                      && i.LateralityCodeId == lateralityCodeId, cancellationToken);

        if (examLateralityGrade == default)
        {
            examLateralityGrade = new ExamLateralityGrade
            {
                ExamId = request.ExamModel.ExamId,
                Gradable = request.Grading.Gradable.HasValue && request.Grading.Gradable.Value,
                LateralityCodeId = lateralityCodeId,
            };

            logger.LogInformation("ExamId:{ExamId} -- Adding Laterality Grade for {LateralityCode}",
                request.ExamModel.ExamId,
                request.LateralityCode);

            examLateralityGrade = (await context.ExamLateralityGrade
                .AddAsync(examLateralityGrade, cancellationToken)).Entity;
            await context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            logger.LogInformation("ExamId:{ExamId} -- Laterality Grade for {LateralityCode} already exists",
                request.ExamModel.ExamId,
                request.LateralityCode);
        }

        return examLateralityGrade.ExamLateralityGradeId;
    }
}