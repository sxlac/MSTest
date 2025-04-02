using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Maps;
using Signify.CKD.Svc.Core.Messages.Status;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.CKD.Svc.Core.Commands
{
    public class AddExamNotPerformed : IRequest<ExamNotPerformed>
    {
        public Data.Entities.CKD Exam { get; }

        public short NotPerformedReasonId { get; }

        public string Notes { get; }

        public AddExamNotPerformed(Data.Entities.CKD exam, short notPerformedReasonId, string notes)
        {
            Exam = exam;
            NotPerformedReasonId = notPerformedReasonId;
            Notes = notes;
        }
    }

    public class AddExamNotPerformedHandler : IRequestHandler<AddExamNotPerformed, ExamNotPerformed>
    {
        private readonly ILogger _logger;
        private readonly CKDDataContext _dataContext;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public AddExamNotPerformedHandler(ILogger<AddExamNotPerformedHandler> logger,
            CKDDataContext dataContext,
            IMapper mapper,
            IMediator mediator)
        {
            _logger = logger;
            _dataContext = dataContext;
            _mapper = mapper;
            _mediator = mediator;
        }

        public async Task<ExamNotPerformed> Handle(AddExamNotPerformed request, CancellationToken cancellationToken)
        {
            var existing = await _dataContext.ExamNotPerformed.FirstOrDefaultAsync(each =>
                    each.CKDId == request.Exam.CKDId,
                cancellationToken);

            if (existing != null)
                return existing;

            var entity = _mapper.Map<ExamNotPerformed>(request.Exam);

            entity.NotPerformedReasonId = request.NotPerformedReasonId;
            entity.Notes = request.Notes;

            entity = (await _dataContext.ExamNotPerformed.AddAsync(entity, cancellationToken)).Entity;

            await _dataContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully inserted a new ExamNotPerformed record for CKDId={CKDId}; new ExamNotPerformedId={ExamNotPerformedId}",
                request.Exam.CKDId, entity.ExamNotPerformedId);

            await PublishStatusUpdate(request, cancellationToken);

            return entity;
        }

        //Tech debt - this doesn't belong here, this should be in the ExamNotPerformedHandler
        private async Task PublishStatusUpdate(AddExamNotPerformed request, CancellationToken cancellationToken)
        {
            var kafkaNotPerformedMessage = _mapper.Map<NotPerformed>(request);

            // CKD Does not indicate whether it is a member or provider refusal yet
            // Nor do we have not performed reason notes yet.
            var reason = await _dataContext.NotPerformedReason.FirstAsync(each =>
                each.NotPerformedReasonId == request.NotPerformedReasonId, cancellationToken);

            // Don't use TryGetValue here, because if it doesn't exist, that's a bigger problem which
            // means that someone added a new NotPerformedReason to the db but did not update this
            // lookup, and we'd be publishing bad data to Kafka. That should throw an exception so
            // we can investigate from the NSB error queue and replay the events after fixed.
            var reasonModel = AnswerToQuestionMap.AnswerTypeMap[reason.AnswerId];
            kafkaNotPerformedMessage.ReasonType = reasonModel.ReasonType;
            kafkaNotPerformedMessage.Reason = reasonModel.Reason;

            kafkaNotPerformedMessage.ReasonNotes = request.Notes;
            await _mediator.Send(new PublishStatusUpdate(kafkaNotPerformedMessage), cancellationToken);
        }
    }
}
