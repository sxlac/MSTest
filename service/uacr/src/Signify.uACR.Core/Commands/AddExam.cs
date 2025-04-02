using MediatR;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.uACR.Core.Commands;

/// <summary>
/// Command to Create or Update a <see cref="Data.Entities.Exam"/> entity in db
/// </summary>
public class AddExam(Exam exam) : IRequest<Exam>
{
    public Exam Exam { get; } = exam;
}

public class AddExamHandler(
    ILogger<AddExamHandler> logger,
    DataContext dataContext)
    : IRequestHandler<AddExam, Exam>
{
    public async Task<Exam> Handle(AddExam request, CancellationToken cancellationToken)
    {
        var entity = (await dataContext.Exams.AddAsync(request.Exam, cancellationToken)
            .ConfigureAwait(false)).Entity;

        await dataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Successfully inserted a new Exam record for EvaluationId={EvaluationId}. New ExamId={ExamId}",
            entity.EvaluationId, entity.ExamId);

        return entity;
    }
}