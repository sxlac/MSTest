using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;
using NotPerformedReason = Signify.eGFR.Core.Models.NotPerformedReason;

namespace Signify.eGFR.Core.Commands;

/// <summary>
/// Command to add details about why a eGFR exam was not performed to database
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
    private readonly ILogger _logger = logger;

    public async Task<ExamNotPerformed> Handle(AddExamNotPerformed request, CancellationToken cancellationToken)
    {
        var entity = mapper.Map<ExamNotPerformed>(request.Exam);
        mapper.Map(request.NotPerformedReason, entity);
        entity.Notes = request.Notes;
        entity = (await dataContext.ExamNotPerformeds.AddAsync(entity, cancellationToken)).Entity;

        await dataContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully inserted a new ExamNotPerformed record for ExamId={ExamId}; new ExamNotPerformedId={ExamNotPerformedId}",
            entity.ExamId, entity.ExamNotPerformedId);

        return entity;
    }
}