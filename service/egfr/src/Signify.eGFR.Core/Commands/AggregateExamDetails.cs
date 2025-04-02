using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using MediatR;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Queries;
using EgfrNsbEvents;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.eGFR.Core.Commands;

/// <summary>
/// Command to aggregate details such as evaluation information, provider information and member information,
/// into a <see cref="Exam"/> object.
/// </summary>
[ExcludeFromCodeCoverage]
public class AggregateExamDetails(EvalReceived @event) : IRequest<Exam>
{
    public EvalReceived EventData { get; } = @event;
}

public class AggregateExamDetailsHandler(
    IMediator mediator,
    IMapper mapper) : IRequestHandler<AggregateExamDetails, Exam>
{
    public async Task<Exam> Handle(AggregateExamDetails request, CancellationToken cancellationToken)
    {
        var exam = mapper.Map<Exam>(request.EventData);

        var providerInfo = await mediator.Send(new QueryProviderInfo(request.EventData.ProviderId), cancellationToken);

        // Don't add ProviderInfo -> exam to the mapper, as to not accidentally set the exam's First/Last
        // name to the provider's name
        exam.NationalProviderIdentifier = providerInfo.NationalProviderIdentifier;

        var memberInfo = await mediator.Send(new QueryMemberInfo(request.EventData.MemberPlanId), cancellationToken);
        mapper.Map(memberInfo, exam);

        return exam;
    }
}