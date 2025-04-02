using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Command to Create a <see cref="Hold"/> entity in db
    /// </summary>
    public class AddHold : IRequest<AddHoldResponse>
    {
        public Hold Hold { get; }

        public AddHold(Hold hold)
        {
            Hold = hold;
        }
    }

    public class AddHoldResponse
    {
        public Hold Hold { get; }

        /// <summary>
        /// Whether this hold was just inserted, or if it already existed in the database
        /// </summary>
        public bool IsNew { get; }

        public AddHoldResponse(Hold hold, bool isNew)
        {
            Hold = hold;
            IsNew = isNew;
        }
    }

    public class AddHoldHandler : IRequestHandler<AddHold, AddHoldResponse>
    {
        private readonly ILogger _logger;
        private readonly SpirometryDataContext _spirometryDataContext;

        public AddHoldHandler(ILogger<AddHoldHandler> logger,
            SpirometryDataContext spirometryDataContext)
        {
            _logger = logger;
            _spirometryDataContext = spirometryDataContext;
        }

        public async Task<AddHoldResponse> Handle(AddHold request, CancellationToken cancellationToken)
        {
            // EvaluationId column of the Hold table has unique constraint
            var entity = await _spirometryDataContext.Holds.FirstOrDefaultAsync(hold => hold.EvaluationId == request.Hold.EvaluationId, cancellationToken);
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

                return new AddHoldResponse(entity, false);
            }

            entity = (await _spirometryDataContext.Holds.AddAsync(request.Hold, cancellationToken)).Entity;

            await _spirometryDataContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully inserted a new Hold record for EvaluationId={EvaluationId}, new HoldId={HoldId}",
                entity.EvaluationId, entity.HoldId);

            return new AddHoldResponse(entity, true);
        }
    }
}
