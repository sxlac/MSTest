using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;

namespace Signify.eGFR.Core.Commands;

/// <summary>
/// Command to add a <see cref="ExamStatus"/> record to database.
/// Note: Do not include any related table objects within <see cref="ExamStatus"/> like <see cref="Exam"/> as they will also be added into the database
/// </summary>
[ExcludeFromCodeCoverage]
public class AddExamStatus(Guid eventId, long evaluationId, ExamStatus status, bool alwaysAddStatus = false)
    : IRequest<AddExamStatusResponse>
{
    public Guid EventId { get; } = eventId;
    public long EvaluationId { get; } = evaluationId;
    public ExamStatus Status { get; } = status;
    public bool AlwaysAddStatus { get; set; } = alwaysAddStatus;
}

/// <summary>
/// Response for <see cref="AddExamStatus"/> command
/// </summary>
[ExcludeFromCodeCoverage]
public class AddExamStatusResponse(ExamStatus status, bool isNew)
{
    public ExamStatus Status { get; } = status;

    /// <summary>
    /// Whether or not this status was just inserted, or if it already existed in the database
    /// </summary>
    public bool IsNew { get; } = isNew;
}

public class AddExamStatusHandler(ILogger<AddExamStatusHandler> logger, DataContext dataContext)
    : IRequestHandler<AddExamStatus, AddExamStatusResponse>
{
    private readonly ILogger _logger = logger;

    [Transaction]
    public async Task<AddExamStatusResponse> Handle(AddExamStatus request, CancellationToken cancellationToken)
    {
        if (!request.AlwaysAddStatus)
        {
            var examStatus = await dataContext.ExamStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                        s.ExamId == request.Status.ExamId && s.ExamStatusCodeId == request.Status.ExamStatusCodeId,
                    cancellationToken);

            if (examStatus != null)
            {
                _logger.LogInformation(
                    "ExamStatus already exists for this evaluation with EvaluationId={EvaluationId} and ExamStatusCodeId={ExamStatusCodeId}; EventId={EventId} returning existing record",
                    request.EvaluationId, request.Status.ExamStatusCodeId, request.EventId);

                return new AddExamStatusResponse(examStatus, false);
            }
        }

        var newExamStatus = (await dataContext.ExamStatuses.AddAsync(request.Status, cancellationToken)).Entity;

        await dataContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully scheduled to insert a new ExamStatus record for EvaluationId={EvaluationId} EventId={EventId}, new ExamStatusId={ExamStatusId}",
            request.EvaluationId, request.EventId, newExamStatus.ExamStatusId);

        return new AddExamStatusResponse(newExamStatus, true);
    }
}