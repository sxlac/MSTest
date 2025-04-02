using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Messages.Events;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.EventHandlers;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.EventHandlers;

public class PADEvaluationReceivedHandlerTests : IClassFixture<EntityFixtures>, IClassFixture<MockDbFixture>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly EntityFixtures _entityFixtures;
    private readonly PadEvaluationReceivedHandler _handler;
    private readonly ITransactionSupplier _transactionSupplier;

    public PADEvaluationReceivedHandlerTests(EntityFixtures entityFixtures, MockDbFixture mockDbFixture)
    {
        _mediator = A.Fake<IMediator>();
        _mapper = A.Fake<IMapper>();
        _entityFixtures = entityFixtures;
        _transactionSupplier = A.Fake<ITransactionSupplier>();
        var publishObservability = A.Fake<IPublishObservability>();
        _handler = new PadEvaluationReceivedHandler(A.Dummy<ILogger<PadEvaluationReceivedHandler>>(), _mediator, mockDbFixture.Context, _mapper, _transactionSupplier, publishObservability);
    }

    private async Task Handle(EvalReceived evalReceived)
    {
        await _handler.Handle(evalReceived, A.Dummy<IMessageHandlerContext>());
    }

    [Fact]
    public async Task EvaluationFinalizedHandler_WhenEvaluationIsAlreadyFinalized()
    {
        EvalReceived @event = new EvalReceived
        {
            EvaluationId = 324357
        };

        await Handle(@event);

        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    private void SetupDefaults()
    {
        A.CallTo(() => _mapper.Map<CreateOrUpdatePAD>(A<EvaluationAnswers>._)).Returns(GetCreateOrUpdatePad);
        A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdatePAD>._)).Returns(GetCreateOrUpdatePad);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
        A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdatePAD>._)).Returns(GetCreateOrUpdatePad);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePAD>._, CancellationToken.None)).Returns(Pad);
        A.CallTo(() => _mediator.Send(A<PADStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockPADStatus());
        A.CallTo(() => _mapper.Map<PADPerformed>(A<Core.Data.Entities.PAD>._)).Returns(_entityFixtures.MockPADPerformed());
    }

    [Fact]
    public async Task EvaluationFinalizedHandler_MembersApiTimesCalled()
    {
        SetupDefaults();

        EvaluationAnswers evaluationAnswers = new EvaluationAnswers
            { LeftScore = "0.28", LeftSeverity = "Severe", RightScore = "0.59", RightSeverity = "Significant", IsPadPerformedToday = true };
        GetMemberInfo memberInfo = new GetMemberInfo { MemberPlanId = EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePAD>._, CancellationToken.None)).Returns(Pad);

        await Handle(EvalReceived);

        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task EvaluationFinalizedHandler_EvaluationApiTimesCalled()
    {
        SetupDefaults();
        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction()).Returns(transaction);

        EvaluationAnswers evaluationAnswers = new EvaluationAnswers
            { LeftScore = "0.28", LeftSeverity = "Severe", RightScore = "0.59", RightSeverity = "Significant", IsPadPerformedToday = true };
        GetMemberInfo memberInfo = new GetMemberInfo { MemberPlanId = EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePAD>._, CancellationToken.None)).Returns(Pad);
        await Handle(EvalReceived);

        A.CallTo(() => _transactionSupplier.BeginTransaction()).MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => transaction.Dispose()).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task EvaluationFinalizedHandler_ProviderApiTimesCalled()
    {
        SetupDefaults();

        EvaluationAnswers evaluationAnswers = new EvaluationAnswers
            { LeftScore = "0.28", LeftSeverity = "Severe", RightScore = "0.59", RightSeverity = "Significant", IsPadPerformedToday = true };
        GetMemberInfo memberInfo = new GetMemberInfo { MemberPlanId = EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePAD>._, CancellationToken.None)).Returns(Pad);

        await Handle(EvalReceived);

        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handler_EvalReceived_CreateAoeSymptomSupportResultCalled()
    {
        // Arrange
        SetupDefaults();
        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction()).Returns(transaction);

        EvaluationAnswers evaluationAnswers = new EvaluationAnswers
            { LeftScore = "0.28", LeftSeverity = "Severe", RightScore = "0.59", RightSeverity = "Significant", IsPadPerformedToday = true };
        GetMemberInfo memberInfo = new GetMemberInfo { MemberPlanId = EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePAD>._, CancellationToken.None)).Returns(Pad);

        // Act
        await Handle(EvalReceived);

        // Assert
        A.CallTo(() => _transactionSupplier.BeginTransaction()).MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => transaction.Dispose()).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateAoeSymptomSupportResult>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishAoeResult>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handler_EvalReceivedWithNoAoeSymptomAnswers_SkipAoeSymptomSupportResultCalled()
    {
        // Arrange
        SetupDefaults();
        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction()).Returns(transaction);

        EvaluationAnswers evaluationAnswers = new EvaluationAnswers
            { LeftScore = "0.28", LeftSeverity = "Severe", RightScore = "0.59", RightSeverity = "Significant", IsPadPerformedToday = true };
        GetMemberInfo memberInfo = new GetMemberInfo { MemberPlanId = EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePAD>._, CancellationToken.None)).Returns(Pad);
        A.CallTo(() => _mapper.Map<AoeSymptomSupportResult>(A<AoeSymptomAnswers>._)).Returns(null);

        // Act
        await Handle(EvalReceived);

        // Assert
        A.CallTo(() => _transactionSupplier.BeginTransaction()).MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => transaction.Dispose()).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateAoeSymptomSupportResult>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishAoeResult>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task EvaluationFinalizedHandler_WhenPADCreationFailed()
    {
        SetupDefaults();
        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction()).Returns(transaction);

        EvaluationAnswers evaluationAnswers = new EvaluationAnswers
            { LeftScore = "0.28", LeftSeverity = "Severe", RightScore = "0.59", RightSeverity = "Significant", IsPadPerformedToday = true };
        GetMemberInfo memberInfo = new GetMemberInfo { MemberPlanId = EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePAD>._, CancellationToken.None))
            .Throws(new Exception());

        await Assert.ThrowsAnyAsync<Exception>(async () => await Handle(EvalReceived));

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<CreatePadStatus>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateNotPerformed>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<Performed>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<Core.Commands.NotPerformed>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => transaction.Dispose())
            .MustHaveHappened();
    }

    [Theory]
    [InlineData(true, 1 /*PADPerformed*/)]
    [InlineData(false, 4 /*PADNotPerformed*/)]
    public async Task Handle_SendsCreatePadStatus(bool isPadPerformedToday, int statusCodeId)
    {
        SetupDefaults();

        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, A<CancellationToken>._)).Returns(new EvaluationAnswers
        {
            IsPadPerformedToday = isPadPerformedToday
        });

        await Handle(EvalReceived);

        A.CallTo(() => _mediator.Send(
                A<CreatePadStatus>.That.Matches(s => s.StatusCode.PADStatusCodeId == statusCodeId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WithNotPerformedAnswerId_SendsCreateNotPerformed()
    {
        SetupDefaults();

        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, A<CancellationToken>._)).Returns(new EvaluationAnswers
        {
            NotPerformedAnswerId = 1
        });

        await Handle(EvalReceived);

        A.CallTo(() => _mediator.Send(
                A<CreateNotPerformed>.That.Matches(x => x.NotPerformedRec.AnswerId == 1),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WithPadPerformed_SendsPadPerformed()
    {
        SetupDefaults();

        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, A<CancellationToken>._)).Returns(new EvaluationAnswers
        {
            IsPadPerformedToday = true
        });

        await Handle(EvalReceived);

        A.CallTo(() => _mediator.Send(A<Performed>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<Core.Commands.NotPerformed>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WithPadNotPerformed_SendsPadNotPerformed()
    {
        SetupDefaults();

        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, A<CancellationToken>._)).Returns(new EvaluationAnswers
        {
            IsPadPerformedToday = false
        });

        await Handle(EvalReceived);

        A.CallTo(() => _mediator.Send(A<Performed>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<Core.Commands.NotPerformed>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WithSideResults_PublishesResults()
    {
        SetupDefaults();

        A.CallTo(() => _mediator.Send(A<QueryEvaluationAnswers>._, A<CancellationToken>._)).Returns(new EvaluationAnswers
        {
            IsPadPerformedToday = true
        });

        A.CallTo(() => _mapper.Map<ResultsReceived>(A<Core.Data.Entities.PAD>._)).Returns(new ResultsReceived
        {
            Results = new List<SideResultInfo>
            {
                new SideResultInfo()
            }
        });

        await Handle(EvalReceived);

        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    private static MemberInfoRs MemberInfoRs => new MemberInfoRs
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

    private static EvalReceived EvalReceived => new EvalReceived
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
        UserName = "vastest1"
    };

    private static CreateOrUpdatePAD GetCreateOrUpdatePad => new()
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
        LeftScoreAnswerValue = "0.28",
        LeftSeverityAnswerValue = "Severe",
        RightScoreAnswerValue = "0.59",
        RightSeverityAnswerValue = "Significant",
        CenseoId = "ADarsh1234",
        City = "Mysuru",
        DateOfBirth = DateTime.UtcNow,

        FirstName = "Adarsh",
        LastName = "H R",
        NationalProviderIdentifier = "1234567890",
        State = "karnataka"
    };

    private static Core.Data.Entities.PAD Pad => new()
    {
        PADId = +10,
        AddressLineOne = "4420 Harpers Ferry Dr",
        AddressLineTwo = "Harpers Ferry Dr",
        ApplicationId = "Signify.Evaluation.Service",
        AppointmentId = 1000084715,
        RightScoreAnswerValue = "0.59",
        RightSeverityAnswerValue = "Significant",
        LeftScoreAnswerValue = "0.28",
        LeftSeverityAnswerValue = "Severe",
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