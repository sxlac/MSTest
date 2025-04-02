using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands
{
    public class UpdateHold : IRequest<UpdateHoldResponse>
    {
        public Guid CdiHoldId { get; }
        public int EvaluationId { get; }
        public DateTime ReleasedOn { get; }

        public UpdateHold(Guid cdiHoldId, int evaluationId, DateTime releasedOn)
        {
            CdiHoldId = cdiHoldId;
            EvaluationId = evaluationId;
            ReleasedOn = releasedOn;
        }
    }

    public class UpdateHoldResponse
    {
        public Hold Hold { get; }

        /// <summary>
        /// Whether the hold was not updated because nothing changed (no-op)
        /// </summary>
        public bool IsNoOp { get; }

        public UpdateHoldResponse(Hold hold, bool isNoOp)
        {
            Hold = hold;
            IsNoOp = isNoOp;
        }
    }

    public class UpdateHoldHandler : IRequestHandler<UpdateHold, UpdateHoldResponse>
    {
        private readonly ILogger _logger;
        private readonly DataContext _dataContext;

        public UpdateHoldHandler(ILogger<UpdateHoldHandler> logger,
            DataContext DataContext)
        {
            _logger = logger;
            _dataContext = DataContext;
        }

        public async Task<UpdateHoldResponse> Handle(UpdateHold request, CancellationToken cancellationToken)
        {
            var entity = await _dataContext.Holds.SingleOrDefaultAsync(e => e.CdiHoldId == request.CdiHoldId, cancellationToken);
            if (entity == null)
                throw new HoldNotFoundException(request.CdiHoldId);

            if (entity.ReleasedDateTime.HasValue)
            {
                _logger.LogInformation("HoldId={HoldId} for EvaluationId={EvaluationId} was already released on {ReleasedDateTime}, no need to update to {RequestedReleasedDateTime}",
                    entity.HoldId, entity.EvaluationId, entity.ReleasedDateTime, request.ReleasedOn);

                return new UpdateHoldResponse(entity, true);
            }

            entity.ReleasedDateTime = request.ReleasedOn;
            await _dataContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated Hold record for EvaluationId={EvaluationId} and HoldId={HoldId}, marking as released on {ReleasedDateTime}",
                entity.EvaluationId, entity.HoldId, request.ReleasedOn);

            return new UpdateHoldResponse(entity, false);
        }
    }
}
