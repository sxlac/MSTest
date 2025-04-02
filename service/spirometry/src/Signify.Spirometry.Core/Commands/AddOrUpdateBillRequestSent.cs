using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Command to save a <see cref="BillRequestSent"/> to database
    /// </summary>
    public class AddOrUpdateBillRequestSent : IRequest<BillRequestSent>
    {
        /// <summary>
        /// Identifier of the event that resulted in this bill request
        /// </summary>
        public Guid EventId { get; }
        /// <summary>
        /// Identifier of the evaluation corresponding to this <see cref="BillRequestSent"/>
        /// </summary>
        public long EvaluationId { get; }
        /// <summary>
        /// Entity to save to database
        /// </summary>
        public BillRequestSent BillRequestSent { get; }

        public AddOrUpdateBillRequestSent(long evaluationId, BillRequestSent billRequestSent)
        {
            EvaluationId = evaluationId;
            BillRequestSent = billRequestSent;
        }
        
        public AddOrUpdateBillRequestSent(Guid eventId, long evaluationId, BillRequestSent billRequestSent)
        {
            EventId = eventId;
            EvaluationId = evaluationId;
            BillRequestSent = billRequestSent;
        }
    }

    public class AddOrUpdateBillRequestSentHandler : IRequestHandler<AddOrUpdateBillRequestSent, BillRequestSent>
    {
        private readonly ILogger _logger;
        private readonly SpirometryDataContext _dataContext;

        public AddOrUpdateBillRequestSentHandler(ILogger<AddOrUpdateBillRequestSentHandler> logger,
            SpirometryDataContext dataContext)
        {
            _logger = logger;
            _dataContext = dataContext;
        }

        public async Task<BillRequestSent> Handle(AddOrUpdateBillRequestSent request, CancellationToken cancellationToken)
        {
            var entity = await FindExisting(request.BillRequestSent);

            if (entity != null)
            {
                _logger.LogInformation("A BillRequestSent record with BillId={BillId} already exists, with BillRequestSentId={BillRequestSentId}, for EvaluationId={EvaluationId}",
                    entity.BillId, entity.BillRequestSentId, request.EvaluationId);
                
                entity = _dataContext.BillRequestSents.Update(request.BillRequestSent).Entity;
                
                await _dataContext.SaveChangesAsync(cancellationToken);
                return entity;
            }

            entity = (await _dataContext.BillRequestSents.AddAsync(request.BillRequestSent, cancellationToken))
                .Entity;

            await _dataContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully inserted a new BillRequestSent record with BillId={BillId} - new BillRequestSentId={BillRequestSentId}, for EvaluationId={EvaluationId}",
                entity.BillId, entity.BillRequestSentId, request.EvaluationId);

            return entity;
        }

        private async Task<BillRequestSent> FindExisting(BillRequestSent billRequestSent)
        {
            return await _dataContext.BillRequestSents
                .AsNoTracking()
                .FirstOrDefaultAsync(each => each.SpirometryExamId == billRequestSent.SpirometryExamId
                                             && each.BillId == billRequestSent.BillId);
        }
    }
}
