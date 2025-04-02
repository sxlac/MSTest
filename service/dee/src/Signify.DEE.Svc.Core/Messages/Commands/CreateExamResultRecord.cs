using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class CreateExamResultRecord(ExamResultModel resultData) : IRequest
{
    public ExamResultModel ResultData { get; set; } = resultData;

    public override string ToString()
        => $"{nameof(ResultData)}: {ResultData}";
}

public class CreateResultRecordCommandHandler(
    ILogger<CreateResultRecordCommandHandler> log,
    IMapper mapper,
    DataContext context)
    : IRequestHandler<CreateExamResultRecord>
{
    [Trace]
    public async Task Handle(CreateExamResultRecord request, CancellationToken cancellationToken)
    {
        var exam = context.Exams.Include(x => x.ExamResults).FirstOrDefault(ex => ex.ExamId == request.ResultData.ExamId);
        if (exam?.EvaluationId == null)
        {
            log.LogInformation("DeeExamId: {ExamId} -- No exam record for exam found in DEE datastore", request.ResultData.ExamId);
            return;
        }

        if (exam.ExamResults != null && exam.ExamResults.Count != 0)
        {
            log.LogInformation("DeeExamId: {ExamId} -- result records found in DEE datastore", request.ResultData.ExamId);
            //return await Task.FromResult(_mapper.Map<ExamResultModel>(@default.ExamResult)).ConfigureAwait(false);
            return;
        }

        //For Testing
        /*
         request.ResultData.Diagnoses.Add("E133212");
         request.ResultData.Diagnoses.Add("E133292");
         request.ResultData.Diagnoses.Add("E133295");


        request.ResultData.RightEyeFindings.Add("Received: true");
        request.ResultData.LeftEyeFindings.Add("Received: true");
        request.ResultData.LeftEyeFindings.Add("Received: false");
        request.ResultData.RightEyeFindings.Add("Received: false");
          */

        var findingNormalityIndicators = new HashSet<string>();

        var result = mapper.Map<ExamResult>(request.ResultData);
        result.NormalityIndicator = Constants.ApplicationConstants.NormalityIndicator.Normal;
        exam.ExamResults.Add(result);

        foreach (var diag in result.ExamDiagnoses) context.Entry(diag).State = EntityState.Added;

        foreach (var find in result.ExamFindings)
        {
            findingNormalityIndicators.Add(find.NormalityIndicator);
            context.Entry(find).State = EntityState.Added;
        }

        if (!request.ResultData.LeftEyeGradable || !request.ResultData.RightEyeGradable)
        {
            result.NormalityIndicator = Constants.ApplicationConstants.NormalityIndicator.Undetermined;
        }

        if (findingNormalityIndicators.Contains(Constants.ApplicationConstants.NormalityIndicator.Undetermined) || findingNormalityIndicators.Count == 0) result.NormalityIndicator = Constants.ApplicationConstants.NormalityIndicator.Undetermined;
        if (findingNormalityIndicators.Contains(Constants.ApplicationConstants.NormalityIndicator.Abnormal)) result.NormalityIndicator = Constants.ApplicationConstants.NormalityIndicator.Abnormal;

        context.Entry(result).State = EntityState.Added;

        await context.SaveChangesAsync(cancellationToken);
    }
}