using AutoMapper;
using MediatR;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using SpiroNsbEvents;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Command to aggregate details such as evaluation information, provider information and member information,
    /// into a <see cref="SpirometryExam"/> object.
    /// </summary>
    public class AggregateSpirometryExamDetails : IRequest<SpirometryExam>
    {
        public EvalReceived EventData { get; }

        public AggregateSpirometryExamDetails(EvalReceived @event)
        {
            EventData = @event;
        }
    }

    public class AggregateSpirometryExamDetailsHandler : IRequestHandler<AggregateSpirometryExamDetails, SpirometryExam>
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public AggregateSpirometryExamDetailsHandler(
            IMediator mediator,
            IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        public async Task<SpirometryExam> Handle(AggregateSpirometryExamDetails request, CancellationToken cancellationToken)
        {
            var spirometryExam = _mapper.Map<SpirometryExam>(request.EventData);

            var providerInfo = await _mediator.Send(new QueryProviderInfo(request.EventData.ProviderId), cancellationToken);

            // Don't add ProviderInfo -> SpirometryExam to the mapper, as to not accidentally set the exam's First/Last
            // name to the provider's name
            spirometryExam.NationalProviderIdentifier = providerInfo.NationalProviderIdentifier;

            var memberInfo = await _mediator.Send(new QueryMemberInfo(request.EventData.MemberPlanId), cancellationToken);
            _mapper.Map(memberInfo, spirometryExam);

            return spirometryExam;
        }
    }
}
