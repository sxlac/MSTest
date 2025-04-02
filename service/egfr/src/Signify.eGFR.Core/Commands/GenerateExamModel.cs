using MediatR;
using Signify.eGFR.Core.Builders;
using Signify.eGFR.Core.Models;
using Signify.eGFR.Core.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.eGFR.Core.Commands;

/// <summary>
/// Command to generate a new <see cref="ExamModel"/> object from evaluation answers queried from the Evaluation API
/// </summary>
public class GenerateExamModel(long evaluationId) : IRequest<ExamModel>
{
    public long EvaluationId { get; } = evaluationId;
}

public class GenerateExamModelHandler(
    IMediator mediator,
    IExamModelBuilder examModelBuilder)
    : IRequestHandler<GenerateExamModel, ExamModel>
{
    public async Task<ExamModel> Handle(GenerateExamModel request, CancellationToken cancellationToken)
    {
        var evaluationModel = await mediator.Send(new QueryEvaluationModel(request.EvaluationId), cancellationToken).ConfigureAwait(false);

        return examModelBuilder
            .ForEvaluation(request.EvaluationId)
            .WithFormVersion(evaluationModel.FormVersionId)
            .WithAnswers(evaluationModel.Answers)
            .WithProviderId(evaluationModel.ProviderId)
            .Build();
    }
}