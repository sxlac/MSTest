using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands
{
    public class CreateHold : IRequest<CreateHoldResponse>
    {
        public Hold Hold { get; }

        public CreateHold(Hold hold)
        {
            Hold = hold;
        }
    }

    public class CreateHoldResponse
    {
        public Hold Hold { get; }

        /// <summary>
        /// Whether this hold was just inserted, or if it already existed in the database
        /// </summary>
        public bool IsNew { get; }

        public CreateHoldResponse(Hold hold, bool isNew)
        {
            Hold = hold;
            IsNew = isNew;
        }
    }

    public class CreateHoldHandler : IRequestHandler<CreateHold, CreateHoldResponse>
    {
        private readonly ILogger _logger;
        private readonly DataContext _dataContext;

        public CreateHoldHandler(ILogger<CreateHoldHandler> logger,
            DataContext DataContext)
        {
            _logger = logger;
            _dataContext = DataContext;
        }

        public async Task<CreateHoldResponse> Handle(CreateHold request, CancellationToken cancellationToken)
        {
            // EvaluationId column of the Hold table has unique constraint
            var entity = await _dataContext.Holds.FirstOrDefaultAsync(hold => hold.EvaluationId == request.Hold.EvaluationId, cancellationToken);
            if (entity != null)
            {
                if (entity.CdiHoldId == request.Hold.CdiHoldId)
                {
                    _logger.LogInformation("Hold record already exists for EvaluationId={EvaluationId}, with CdiHoldId={CdiHoldId}, which was created {TimeDiff} ago",
                        entity.EvaluationId, entity.HoldId, request.Hold.CreatedDateTime - entity.CreatedDateTime);
                }
                else
                {
                    // If this log is ever found, it's likely a bug in CDI, as evaluations should only ever have a single hold for a given product
                    _logger.LogWarning("EvaluationId={EvaluationId} already has a hold record with CdiHoldId={CdiHoldId}, which was created {TimeDiff} ago, ignoring new {NewCdiHoldId} hold for this evaluation",
                        entity.EvaluationId, entity.CdiHoldId, request.Hold.CreatedDateTime - entity.CreatedDateTime, request.Hold.CdiHoldId);
                }

                return new CreateHoldResponse(entity, false);
            }

            entity = (await _dataContext.Holds.AddAsync(request.Hold, cancellationToken)).Entity;

            await _dataContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully inserted a new Hold record for EvaluationId={EvaluationId}, new HoldId={HoldId}",
                entity.EvaluationId, entity.HoldId);

            return new CreateHoldResponse(entity, true);
        }
    }
}
