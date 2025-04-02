using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.EventHandlers;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;
using FOBTEntities = Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers;

public class EvalutaionReceivedHandlerTests : IClassFixture<EntityFixtures>, IClassFixture<MockDbFixture>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly EntityFixtures _entityFixtures;
    private readonly EvaluationReceivedHandler _handler;
    private readonly TestableMessageHandlerContext _messageHandlerContext;

    public EvalutaionReceivedHandlerTests(EntityFixtures entityFixtures, MockDbFixture mobFobtFixture)
    {
        var logger = A.Fake<ILogger<EvaluationReceivedHandler>>();
        _mediator = A.Fake<IMediator>();
        _mapper = A.Fake<IMapper>();
        _entityFixtures = entityFixtures;
        _messageHandlerContext = new TestableMessageHandlerContext();
        var publishObservability = A.Fake<IPublishObservability>();
        _handler = new EvaluationReceivedHandler(logger, _mediator, mobFobtFixture.Context, _mapper, publishObservability);
    }

    [Fact]
    public async Task Should_Ignore_WhenEvaluationIsAlreadyFinalized()
    {
        var @event = new EvaluationReceived
        {
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084716,
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfService = DateTime.UtcNow,
            DocumentPath = null,
            EvaluationId = 324357,
            EvaluationTypeId = 1,
            FormVersionId = 0,
            MemberId = 11990396,
            MemberPlanId = 21074285,
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            UserName = "vastest1"
        };

        await _handler.Handle(@event, _messageHandlerContext);
        _messageHandlerContext.PublishedMessages.Length.Should().Be(0);
    }

    [Fact]

    public async Task Should_Publish_A1CPerformedEvent()
    {
        var memberInfo = new GetMemberInfo { MemberPlanId = EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<QueryFOBT>._, CancellationToken.None)).Returns(_entityFixtures.MockQueryFOBTResponse());
        A.CallTo(() => _mapper.Map(A<EvaluationReceived>._, A<CreateOrUpdateFOBT>._)).Returns(GetCreateOrUpdateFobt);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvaluationReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
        A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateFOBT>._)).Returns(GetCreateOrUpdateFobt);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateFOBT>._, CancellationToken.None)).Returns(Fobt);
        A.CallTo(() => _mediator.Send(A<FOBTEntities.FOBTStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockFOBTStatus());
        A.CallTo(() => _mapper.Map<FOBTPerformedEvent>(A<FOBTEntities.FOBT>._)).Returns(_entityFixtures.MockFOBTPerformed());            

        await _handler.Handle(EvalReceived, _messageHandlerContext);
        _messageHandlerContext.PublishedMessages.Length.Should().Be(1);
    }

    [Fact]
    public async Task EvaluationReceivedHandler_PublishTypeCheck()
    {
        var memberInfo = new GetMemberInfo { MemberPlanId = EvalReceived.MemberPlanId };

        A.CallTo(() => _mediator.Send(A<QueryFOBT>._, CancellationToken.None)).Returns(_entityFixtures.MockQueryFOBTResponse());
        A.CallTo(() => _mapper.Map(A<EvaluationReceived>._, A<CreateOrUpdateFOBT>._)).Returns(GetCreateOrUpdateFobt);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvaluationReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
        A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateFOBT>._)).Returns(GetCreateOrUpdateFobt);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateFOBT>._, CancellationToken.None)).Returns(Fobt);
        A.CallTo(() => _mediator.Send(A<FOBTEntities.FOBTStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockFOBTStatus());
        A.CallTo(() => _mapper.Map<FOBTPerformedEvent>(A<FOBTEntities.FOBT>._)).Returns(_entityFixtures.MockFOBTPerformed());

        await _handler.Handle(EvalReceived, _messageHandlerContext);
        _messageHandlerContext.PublishedMessages[0].Message.Should().BeOfType<FOBTPerformedEvent>();
    }

    [Fact]
    public async Task EvaluationReceivedHandler_MembersApiTimesCalled()
    {
        var memberInfo = new GetMemberInfo { MemberPlanId = EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<QueryFOBT>._, CancellationToken.None)).Returns(_entityFixtures.MockQueryFOBTResponse());
        A.CallTo(() => _mapper.Map(A<EvaluationReceived>._, A<CreateOrUpdateFOBT>._)).Returns(GetCreateOrUpdateFobt);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvaluationReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
        A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateFOBT>._)).Returns(GetCreateOrUpdateFobt);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateFOBT>._, CancellationToken.None)).Returns(Fobt);
        A.CallTo(() => _mediator.Send(A<FOBTEntities.FOBTStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockFOBTStatus());
        A.CallTo(() => _mapper.Map<FOBTPerformedEvent>(A<FOBTEntities.FOBT>._)).Returns(_entityFixtures.MockFOBTPerformed());

        await _handler.Handle(EvalReceived, _messageHandlerContext);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task EvaluationReceivedHandler_ProviderApiTimesCalled()
    {
        var memberInfo = new GetMemberInfo { MemberPlanId = EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<QueryFOBT>._, CancellationToken.None)).Returns(_entityFixtures.MockQueryFOBTResponse());
        A.CallTo(() => _mapper.Map(A<EvaluationReceived>._, A<CreateOrUpdateFOBT>._)).Returns(GetCreateOrUpdateFobt);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvaluationReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
        A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateFOBT>._)).Returns(GetCreateOrUpdateFobt);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateFOBT>._, CancellationToken.None)).Returns(Fobt);
        A.CallTo(() => _mediator.Send(A<FOBTEntities.FOBTStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockFOBTStatus());
        A.CallTo(() => _mapper.Map<FOBTPerformedEvent>(A<FOBTEntities.FOBT>._)).Returns(_entityFixtures.MockFOBTPerformed());

        await _handler.Handle(EvalReceived, _messageHandlerContext);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task EvaluationReceivedHandler_WhenA1CCreationFailed()
    {
        var memberInfo = new GetMemberInfo { MemberPlanId = EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<QueryFOBT>._, CancellationToken.None)).Returns(_entityFixtures.MockQueryFOBTResponse());
        A.CallTo(() => _mapper.Map(A<EvaluationReceived>._, A<CreateOrUpdateFOBT>._)).Returns(GetCreateOrUpdateFobt);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvaluationReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
        A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateFOBT>._)).Returns(GetCreateOrUpdateFobt);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateFOBT>._, CancellationToken.None)).Returns(new FOBTEntities.FOBT());
        A.CallTo(() => _mediator.Send(A<FOBTEntities.FOBTStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockFOBTStatus());
        A.CallTo(() => _mapper.Map<FOBTPerformedEvent>(A<FOBTEntities.FOBT>._)).Returns(_entityFixtures.MockFOBTPerformed());

        await _handler.Handle(EvalReceived, _messageHandlerContext);
        _messageHandlerContext.PublishedMessages.Length.Should().Be(0);
    }

    private static MemberInfoRs MemberInfoRs => new()
    {
        AddressLineOne = "Raghavendra nagara",
        AddressLineTwo = "mysuru",
        CenseoId = "ADarsh1234",
        City = "Mysuru",
        DateOfBirth = DateTime.UtcNow,
        FirstName = "Adarsh",
        LastName = "H R",
        State = "karnataka",
        ZipCode = "12345",
        Client = "14",
        MiddleName = ""
    };
    private static EvaluationReceived EvalReceived => new()
    {
        ApplicationId = "Signify.Evaluation.Service",
        AppointmentId = 1000084716,
        ClientId = 14,
        CreatedDateTime = DateTimeOffset.UtcNow,
        DateOfService = DateTime.UtcNow,
        DocumentPath = null,
        EvaluationId = 324359,
        EvaluationTypeId = 1,
        FormVersionId = 0,
        MemberId = 11990396,
        MemberPlanId = 21074285,
        ProviderId = 42879,
        ReceivedDateTime = DateTime.UtcNow,
        UserName = "vastest1",
        Performed = true
    };

    private static CreateOrUpdateFOBT GetCreateOrUpdateFobt => new()
    {
        AddressLineOne = "Raghavendra nagara",
        AddressLineTwo = "mysuru",
        ApplicationId = "Signify.Evaluation.Service",
        AppointmentId = 1000084716,
        ClientId = 14,
        CreatedDateTime = DateTimeOffset.UtcNow,
        DateOfService = DateTime.UtcNow,
        EvaluationId = 324359,
        MemberId = 11990396,
        MemberPlanId = 21074285,
        ProviderId = 42879,
        ReceivedDateTime = DateTime.UtcNow,
        CenseoId = "ADarsh1234",
        City = "Mysuru",
        DateOfBirth = DateTime.UtcNow,
        FirstName = "Adarsh",
        LastName = "H R",
        NationalProviderIdentifier = "1234567890",
        State = "karnataka"
    };

    private static FOBTEntities.FOBT Fobt => new()
    {
        FOBTId = +10,
        AddressLineOne = "4420 Harpers Ferry Dr",
        AddressLineTwo = "Harpers Ferry Dr",
        ApplicationId = "Signify.Evaluation.Service",
        AppointmentId = 1000084715,
        CenseoId = "Adarsh1234",
        City = "Mysuru",
        ClientId = 14,
        CreatedDateTime = DateTimeOffset.UtcNow,
        DateOfBirth = DateTime.UtcNow,
        DateOfService = DateTime.UtcNow,
        EvaluationId = 324356,
        FirstName = "Adarsh",
        LastName = "H R",
        MemberId = 11990396,
        MemberPlanId = 21074285,
        NationalProviderIdentifier = "9230239051",
        ProviderId = 42879,
        ReceivedDateTime = DateTime.UtcNow,
        State = "Karnataka",
        UserName = "vastest1",
        ZipCode = "12345"
    };
}