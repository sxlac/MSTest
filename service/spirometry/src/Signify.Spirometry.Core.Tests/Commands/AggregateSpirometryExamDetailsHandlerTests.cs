using AutoMapper;
using FakeItEasy;
using MediatR;
using Signify.Spirometry.Core.ApiClients.MemberApi.Responses;
using Signify.Spirometry.Core.ApiClients.ProviderApi.Responses;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using SpiroNsbEvents;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public class AggregateSpirometryExamDetailsHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();

    private AggregateSpirometryExamDetailsHandler CreateSubject() => new(_mediator, _mapper);

    [Fact]
    public async Task Handle_WithProviderId_DoesNotUseMapperForNationalProviderInfo()
    {
        // If the flow changes and we do decide to use mapper for this property and remove this unit test,
        // we need to add unit tests to ensure the resulting SpirometryExam's FirstName and LastName
        // properties do NOT get set to the NPI's FirstName and LastName.

        const int providerId = 1;
        const string nationalProviderIdentifier = "n";

        var request = new AggregateSpirometryExamDetails(
            new EvalReceived
            {
                ProviderId = providerId
            });

        A.CallTo(() => _mediator.Send(A<QueryProviderInfo>._, A<CancellationToken>._))
            .Returns(Task.FromResult(new ProviderInfo {NationalProviderIdentifier = nationalProviderIdentifier}));

        var subject = CreateSubject();

        var result = await subject.Handle(request, CancellationToken.None);

        A.CallTo(() => _mediator.Send(A<QueryProviderInfo>.That.Matches(
                q => q.ProviderId == providerId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _mapper.Map<SpirometryExam>(A<ProviderInfo>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mapper.Map(A<ProviderInfo>._, A<SpirometryExam>._))
            .MustNotHaveHappened();

        Assert.Equal(nationalProviderIdentifier, result.NationalProviderIdentifier);
        Assert.Null(result.FirstName);
        Assert.Null(result.LastName);
    }

    [Fact]
    public async Task Handle_WithValidRequest_SendsAllQueriesAndPerformsMaps()
    {
        const int providerId = 1, memberPlanId = 2;
        const string nationalProviderIdentifier = "n";

        var request = new AggregateSpirometryExamDetails(
            new EvalReceived
            {
                ProviderId = providerId,
                MemberPlanId = memberPlanId
            });

        var spirometryExam = new SpirometryExam();
        var providerInfo = new ProviderInfo
        {
            NationalProviderIdentifier = nationalProviderIdentifier
        };
        var memberInfo = new MemberInfo();

        A.CallTo(() => _mapper.Map<SpirometryExam>(A<EvalReceived>._))
            .Returns(spirometryExam);
        A.CallTo(() => _mediator.Send(A<QueryProviderInfo>._, A<CancellationToken>._))
            .Returns(providerInfo);
        A.CallTo(() => _mediator.Send(A<QueryMemberInfo>._, A<CancellationToken>._))
            .Returns(memberInfo);
        A.CallTo(() => _mapper.Map(A<MemberInfo>._, A<SpirometryExam>._))
            .Returns(spirometryExam);

        var subject = CreateSubject();

        var result = await subject.Handle(request, CancellationToken.None);

        A.CallTo(() => _mapper.Map<SpirometryExam>(request.EventData))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<QueryProviderInfo>.That.Matches(
                q => q.ProviderId == providerId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<QueryMemberInfo>.That.Matches(
                q => q.MemberPlanId == memberPlanId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map(memberInfo, spirometryExam))
            .MustHaveHappenedOnceExactly();

        Assert.Equal(spirometryExam, result);
        Assert.Equal(nationalProviderIdentifier, result.NationalProviderIdentifier);
    }
}