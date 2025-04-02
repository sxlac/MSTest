using MediatR;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.eGFR.Core.Commands;

    /// <summary>
    /// Command to Create a <see cref="QuestLabResult"/> entity in db
    /// </summary>
    public class AddQuestLabResult(QuestLabResult questLabResult) : IRequest<QuestLabResult>
    {
        public QuestLabResult QuestLabResult { get; } = questLabResult;
    }

    public class AddQuestLabResultHandler(
        ILogger<AddQuestLabResultHandler> logger,
        DataContext dataContext)
        : IRequestHandler<AddQuestLabResult, QuestLabResult>
    {
        private readonly ILogger _logger = logger;

        public async Task<QuestLabResult> Handle(AddQuestLabResult request, CancellationToken cancellationToken)
        {
            var entity = (await dataContext.QuestLabResults.AddAsync(request.QuestLabResult, cancellationToken)
                .ConfigureAwait(false)).Entity;

            await dataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully inserted a new Quest LabResult record. New LabResultId={LabResultId}", entity.LabResultId);

            return entity;
        }
    }