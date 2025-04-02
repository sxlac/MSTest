using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;
using NotPerformedReason = Signify.uACR.Core.Models.NotPerformedReason;

namespace Signify.uACR.Core.Commands;

/// <summary>
/// Command to add details about why a UACR exam was not performed to database
/// </summary>
public class AddExamNotPerformed(Exam exam, NotPerformedReason notPerformedReason, string notes) : IRequest<ExamNotPerformed>
{
    public Exam Exam { get; } = exam;

    public NotPerformedReason NotPerformedReason { get; } = notPerformedReason;

    public string Notes { get; } = notes;
}

public class AddExamNotPerformedHandler(
    ILogger<AddExamNotPerformedHandler> logger,
    DataContext dataContext,
    IMapper mapper)
    : IRequestHandler<AddExamNotPerformed, ExamNotPerformed>
{
    public async Task<ExamNotPerformed> Handle(AddExamNotPerformed request, CancellationToken cancellationToken)
    {
        var entity = mapper.Map<ExamNotPerformed>(request.Exam);
        mapper.Map(request.NotPerformedReason, entity);
        entity.Notes = request.Notes;
        entity = (await dataContext.ExamNotPerformeds.AddAsync(entity, cancellationToken)).Entity;

        await dataContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully inserted a new ExamNotPerformed record for ExamId={ExamId}; new ExamNotPerformedId={ExamNotPerformedId}",
            entity.ExamId, entity.ExamNotPerformedId);

        return entity;
    }
}