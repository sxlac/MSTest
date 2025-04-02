using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Svc.Core.Parsers;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.StaticEntity;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class HBA1CPOCEvaluationReceivedHandlerTest : IClassFixture<MockDbFixture>
{
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IResultsParser _parser = A.Fake<IResultsParser>();
    private readonly HBA1CPOCEvaluationReceivedHandler _hbA1CPocEvaluationReceivedHandler;
    private readonly TestableMessageHandlerContext _messageHandlerContext = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    public HBA1CPOCEvaluationReceivedHandlerTest(MockDbFixture mockDbFixture)
    {
        _hbA1CPocEvaluationReceivedHandler = new HBA1CPOCEvaluationReceivedHandler(A.Dummy<ILogger<HBA1CPOCEvaluationReceivedHandler>>(),
            _transactionSupplier, _mediator, _mapper, _parser, _publishObservability);

        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, A<CancellationToken>._))
            .ReturnsLazily(call =>
            {
                var request = call.GetArgument<GetHBA1CPOC>("request");
                return mockDbFixture.Context.HBA1CPOC
                    .AsNoTracking()
                    .FirstOrDefaultAsync(each => each.EvaluationId == request.EvaluationId);
            });
    }

    [Fact]
    public async Task EvaluationReceivedHandler_WhenEvaluationIsAlreadyFinalized()
    {
        var @event = new EvalReceived
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
        await _hbA1CPocEvaluationReceivedHandler.Handle(@event, _messageHandlerContext);
        _messageHandlerContext.PublishedMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task EvaluationReceivedHandler_PublishCheck()
    {
        var evaluationAnswers = new EvaluationAnswers
            { A1CPercent = "6", ExpirationDate = DateTime.UtcNow, IsHBA1CEvaluation = true };
        var memberInfo = new GetMemberInfo { MemberPlanId = StaticMockEntities.EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<CreateOrUpdateHBA1CPOC>(A<EvaluationAnswers>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(new ProviderRs());
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(StaticMockEntities.MemberInfoRs);
        A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, CancellationToken.None))
            .Returns(StaticMockEntities.Hba1Cpoc);
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None)).Returns(EntityFixtures.MockCreateHBA1CPOCStatus());
        A.CallTo(() => _mapper.Map<A1CPOCPerformed>(A<Core.Data.Entities.HBA1CPOC>._)).Returns(new A1CPOCPerformed());
        await _hbA1CPocEvaluationReceivedHandler.Handle(StaticMockEntities.EvalReceived, _messageHandlerContext);
        _messageHandlerContext.PublishedMessages.Length.Should().Be(1);

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task EvaluationReceivedHandler_PublishTypeCheck()
    {
        var evaluationAnswers = new EvaluationAnswers
            { A1CPercent = "6", ExpirationDate = DateTime.UtcNow, IsHBA1CEvaluation = true };
        var memberInfo = new GetMemberInfo { MemberPlanId = StaticMockEntities.EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<CreateOrUpdateHBA1CPOC>(A<EvaluationAnswers>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(new ProviderRs());
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(StaticMockEntities.MemberInfoRs);
        A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, CancellationToken.None)).Returns(StaticMockEntities.Hba1Cpoc);
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None)).Returns(EntityFixtures.MockCreateHBA1CPOCStatus());
        A.CallTo(() => _mapper.Map<A1CPOCPerformed>(A<Core.Data.Entities.HBA1CPOC>._)).Returns(new A1CPOCPerformed());
        await _hbA1CPocEvaluationReceivedHandler.Handle(StaticMockEntities.EvalReceived, _messageHandlerContext);
        _messageHandlerContext.PublishedMessages[0].Message.Should().BeOfType<A1CPOCPerformed>();

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task EvaluationReceivedHandler_MembersApiTimesCalled()
    {
        var evaluationAnswers = new EvaluationAnswers
            { A1CPercent = "6", ExpirationDate = DateTime.UtcNow, IsHBA1CEvaluation = true };
        var memberInfo = new GetMemberInfo { MemberPlanId = StaticMockEntities.EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<CreateOrUpdateHBA1CPOC>(A<EvaluationAnswers>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(new ProviderRs());
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(StaticMockEntities.MemberInfoRs);
        A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, CancellationToken.None)).Returns(StaticMockEntities.Hba1Cpoc);
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None)).Returns(EntityFixtures.MockCreateHBA1CPOCStatus());
        A.CallTo(() => _mapper.Map<A1CPOCPerformed>(A<Core.Data.Entities.HBA1CPOC>._)).Returns(new A1CPOCPerformed());
        await _hbA1CPocEvaluationReceivedHandler.Handle(StaticMockEntities.EvalReceived, _messageHandlerContext);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task EvaluationReceivedHandler_EvaluationApiTimesCalled()
    {
        var evaluationAnswers = new EvaluationAnswers
            { A1CPercent = "6", ExpirationDate = DateTime.UtcNow, IsHBA1CEvaluation = true };
        var memberInfo = new GetMemberInfo { MemberPlanId = StaticMockEntities.EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<CreateOrUpdateHBA1CPOC>(A<EvaluationAnswers>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(new ProviderRs());
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(StaticMockEntities.MemberInfoRs);
        A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, CancellationToken.None)).Returns(StaticMockEntities.Hba1Cpoc);
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None)).Returns(EntityFixtures.MockCreateHBA1CPOCStatus());
        A.CallTo(() => _mapper.Map<A1CPOCPerformed>(A<Core.Data.Entities.HBA1CPOC>._)).Returns(new A1CPOCPerformed());
        await _hbA1CPocEvaluationReceivedHandler.Handle(StaticMockEntities.EvalReceived, _messageHandlerContext);
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task EvaluationReceivedHandler_ProviderApiTimesCalled()
    {
        var evaluationAnswers = new EvaluationAnswers
            { A1CPercent = "6", ExpirationDate = DateTime.UtcNow, IsHBA1CEvaluation = true };
        var memberInfo = new GetMemberInfo { MemberPlanId = StaticMockEntities.EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<CreateOrUpdateHBA1CPOC>(A<EvaluationAnswers>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(new ProviderRs());
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(StaticMockEntities.MemberInfoRs);
        A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, CancellationToken.None)).Returns(StaticMockEntities.Hba1Cpoc);
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None)).Returns(EntityFixtures.MockCreateHBA1CPOCStatus());
        A.CallTo(() => _mapper.Map<A1CPOCPerformed>(A<Core.Data.Entities.HBA1CPOC>._)).Returns(new A1CPOCPerformed());
        await _hbA1CPocEvaluationReceivedHandler.Handle(StaticMockEntities.EvalReceived, _messageHandlerContext);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task EvaluationReceivedHandler_WhenHBA1CPOCCreationFailed()
    {
        var evaluationAnswers = new EvaluationAnswers
            { A1CPercent = "6", ExpirationDate = DateTime.UtcNow, IsHBA1CEvaluation = true };
        var memberInfo = new GetMemberInfo { MemberPlanId = StaticMockEntities.EvalReceived.MemberPlanId };
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, CancellationToken.None)).Returns(evaluationAnswers);
        A.CallTo(() => _mapper.Map<CreateOrUpdateHBA1CPOC>(A<EvaluationAnswers>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(new ProviderRs());
        A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
        A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(StaticMockEntities.MemberInfoRs);
        A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateHBA1CPOC>._)).Returns(StaticMockEntities.CreateOrUpdateHba1Cpoc);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, CancellationToken.None)).Returns(new Core.Data.Entities.HBA1CPOC());
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None)).Returns(EntityFixtures.MockCreateHBA1CPOCStatus());
        A.CallTo(() => _mapper.Map<A1CPOCPerformed>(A<Core.Data.Entities.HBA1CPOC>._)).Returns(new A1CPOCPerformed());
        await _hbA1CPocEvaluationReceivedHandler.Handle(StaticMockEntities.EvalReceived, _messageHandlerContext);
        _messageHandlerContext.PublishedMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenLabNotPerformed_DoesNotPublishA1CPOCPerformed()
    {
        var eval = new EvalReceived
        {
            EvaluationId = StaticMockEntities.EvalReceived.EvaluationId,
            ProviderId = StaticMockEntities.EvalReceived.ProviderId
        };

        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, A<CancellationToken>._))
            .Returns(new EvaluationAnswers { IsHBA1CEvaluation = false });

        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.HBA1CPOC { HBA1CPOCId = 1 });

        await _hbA1CPocEvaluationReceivedHandler.Handle(eval, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>.That.Matches(
                s => s.StatusCodeId.Equals(HBA1CPOCStatusCode.HBA1CPOCNotPerformed.HBA1CPOCStatusCodeId)), A<CancellationToken>._)
        ).MustHaveHappened();

        _messageHandlerContext.PublishedMessages.Should().BeEmpty();

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_WhenNotPerformed_AndHasNotPerformedReason_AddsNotPerformedReason()
    {
        const int formAnswerId = 1;
        const string reason = "reason";
        const string reasonType = "Member Refused";

        var eval = new EvalReceived
        {
            EvaluationId = StaticMockEntities.EvalReceived.EvaluationId,
            ProviderId = StaticMockEntities.EvalReceived.ProviderId
        };

        var nprr = new NotPerformedReasonResult
        (
            new NotPerformedReason { AnswerId = formAnswerId, NotPerformedReasonId = 1, Reason = "Test" },
            reason,
            "Test",
            "Member Refused"
        );

        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, A<CancellationToken>._))
            .Returns(new EvaluationAnswers { IsHBA1CEvaluation = false });

        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.HBA1CPOC { HBA1CPOCId = 1 });

        A.CallTo(() => _mediator.Send(A<GetNotPerformedReason>._, A<CancellationToken>._))
            .Returns(nprr);

        await _hbA1CPocEvaluationReceivedHandler.Handle(eval, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<GetNotPerformedReason>.That.Matches(g =>
                    g.EvaluationId == eval.EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<AddHba1CpocNotPerformed>.That.Matches(a =>
                    a.NotPerformedReason == nprr.NotPerformedReason && a.HBA1CPOC != null),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                    p.Status is NotPerformed && ((NotPerformed)p.Status).ReasonType == reasonType),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                p.Status is BillRequestNotSent), A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenNotPerformed_AndDoesNotHaveNotPerformedReason_DoesNotAddNotPerformedReason()
    {
        var eval = new EvalReceived
        {
            EvaluationId = StaticMockEntities.EvalReceived.EvaluationId,
            ProviderId = StaticMockEntities.EvalReceived.ProviderId
        };

        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, A<CancellationToken>._))
            .Returns(new EvaluationAnswers { IsHBA1CEvaluation = false });

        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.HBA1CPOC { HBA1CPOCId = 1 });

        A.CallTo(() => _mediator.Send(A<GetNotPerformedReason>._, A<CancellationToken>._))
            .Returns((NotPerformedReasonResult)null);

        await _hbA1CPocEvaluationReceivedHandler.Handle(eval, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<GetNotPerformedReason>.That.Matches(g =>
                    g.EvaluationId == eval.EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<AddHba1CpocNotPerformed>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                    p.Status is NotPerformed && ((NotPerformed)p.Status).ReasonNotes == default),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                p.Status is BillRequestNotSent), A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenNotPerformed_PublishesNotPerformed()
    {
        var eval = new EvalReceived
        {
            EvaluationId = StaticMockEntities.EvalReceived.EvaluationId,
            ProviderId = StaticMockEntities.EvalReceived.ProviderId
        };

        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, A<CancellationToken>._))
            .Returns(new EvaluationAnswers { IsHBA1CEvaluation = false });

        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.HBA1CPOC { HBA1CPOCId = 1 });

        await _hbA1CPocEvaluationReceivedHandler.Handle(eval, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(u =>
                    u.Status is NotPerformed),
                A<CancellationToken>._))
            .MustHaveHappened();

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_WhenLabPerformed_PublishesA1CPOCPerformed()
    {
        // Arrange
        var eval = new EvalReceived
        {
            EvaluationId = StaticMockEntities.EvalReceived.EvaluationId,
            ProviderId = StaticMockEntities.EvalReceived.ProviderId
        };

        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, A<CancellationToken>._))
            .Returns(new EvaluationAnswers { IsHBA1CEvaluation = true });

        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.HBA1CPOC { HBA1CPOCId = 1 });

        // Act
        await _hbA1CPocEvaluationReceivedHandler.Handle(eval, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>.That.Matches(
                s => s.StatusCodeId.Equals(HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId)), A<CancellationToken>._)
        ).MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(u =>
                    u.Status is Performed),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        _messageHandlerContext.PublishedMessages.Should().ContainSingle();

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_WhenLabPerformed_PublishesResults()
    {
        // Arrange
        var eval = new EvalReceived
        {
            EvaluationId = StaticMockEntities.EvalReceived.EvaluationId,
            ProviderId = StaticMockEntities.EvalReceived.ProviderId
        };

        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, A<CancellationToken>._))
            .Returns(new EvaluationAnswers { IsHBA1CEvaluation = true, A1CPercent = "<4" });

        A.CallTo(() => _mediator.Send(A<CreateOrUpdateHBA1CPOC>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.HBA1CPOC { HBA1CPOCId = 1 });

        A.CallTo(() => _parser.Parse(A<string>._))
            .Returns(new ResultsModel());

        // Act
        await _hbA1CPocEvaluationReceivedHandler.Handle(eval, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mapper.Map<ResultsReceived>(A<Core.Data.Entities.HBA1CPOC>.That.Matches(e =>
                e.HBA1CPOCId == 1)))
            .MustHaveHappened();

        A.CallTo(() => _mapper.Map(A<ResultsModel>._, A<ResultsReceived>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<PublishLabResults>.That.Matches(p => p.Results != null), A<CancellationToken>._))
            .MustHaveHappened();

        _transactionSupplier.AssertCommit();
    }
}