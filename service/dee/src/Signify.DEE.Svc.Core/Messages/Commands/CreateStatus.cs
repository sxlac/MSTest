using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class CreateStatus : IRequest<CreateStatusResponse>
{
    public CreateStatus()
    {
    }

    public CreateStatus(int examId, string statusCode, DateTimeOffset messageDateTime, bool alwaysAddStatus = false)
    {
        ExamId = examId;
        ExamStatusCode = ExamStatusCode.Create(statusCode);
        MessageDateTime = messageDateTime;
        AlwaysAddStatus = alwaysAddStatus;
    }

    public int ExamId { get; set; }
    public ExamStatusCode ExamStatusCode { get; set; }
    public DateTimeOffset MessageDateTime { get; set; }
    public bool AlwaysAddStatus { get; set; }
}

/// <summary>
/// Response for <see cref="CreateStatus"/> command
/// </summary>
public class CreateStatusResponse(ExamStatus examStatus, bool isNew)
{
    public ExamStatus ExamStatus { get; set; } = examStatus;

    /// <summary>
    /// Whether or not this status was just inserted, or if it already existed in the database
    /// </summary>
    public bool IsNew { get; set; } = isNew;
}

public class CreateStatusHandler(
    ILogger<CreateStatusHandler> log,
    IApplicationTime applicationTime,
    DataContext context)
    : IRequestHandler<CreateStatus, CreateStatusResponse>
{
    [Trace]
    public async Task<CreateStatusResponse> Handle(CreateStatus request, CancellationToken cancellationToken)
    {
        using var scope = log.BeginScope("ExamId={ExamId}, ExamStatusCode={ExamStatusCode}", request.ExamId, request.ExamStatusCode);
        if (!request.AlwaysAddStatus)
        {
            var examStatus = await context.ExamStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                        x.ExamId == request.ExamId && x.ExamStatusCodeId == request.ExamStatusCode.ExamStatusCodeId,
                    cancellationToken);
            if (examStatus != null)
            {
                log.LogInformation("ExamId:{ExamId} -- Exam Status History already contains status {Name}",
                    examStatus.ExamId,
                    request.ExamStatusCode.Name);

                return new CreateStatusResponse(examStatus, false);
            }
        }

        var newExamStatus = new ExamStatus
        {
            ExamId = request.ExamId,
            CreatedDateTime = applicationTime.UtcNow(),
            ReceivedDateTime = request.MessageDateTime,
            ExamStatusCodeId = request.ExamStatusCode.ExamStatusCodeId
        };

        var addedStatus = (await context.ExamStatuses.AddAsync(newExamStatus, cancellationToken)).Entity;

        await context.SaveChangesAsync(cancellationToken);

        log.LogInformation("ExamId:{ExamId} -- Added {Name} status to Exam",
            addedStatus.ExamId,
            request.ExamStatusCode.Name);

        return new CreateStatusResponse(addedStatus, true);
    }
}