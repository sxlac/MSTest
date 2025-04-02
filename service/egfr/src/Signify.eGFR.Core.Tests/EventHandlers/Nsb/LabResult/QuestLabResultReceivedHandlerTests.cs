using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.BusinessRules;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Infrastructure;
using Signify.eGFR.Core.Models;
using Signify.eGFR.Core.Queries;
using Xunit;
using NormalityIndicator = Signify.eGFR.Core.Configs.NormalityIndicator;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.LabResult;

public class QuestQuestLabResultReceivedHandlerTests
{
    private readonly IMapper _fakeMapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly FakeApplicationTime _applicationTime = new();
    private NormalityIndicator _normalityIndicator = A.Fake<NormalityIndicator>();
    private readonly TestableMessageHandlerContext _fakeContext = new();
    private readonly IFeatureFlags _featureFlags = A.Fake<IFeatureFlags>();
    private readonly IBillableRules _billableRules = A.Fake<IBillableRules>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();

    private QuestLabResultReceivedHandler CreateSubject()
    {
        _normalityIndicator = new NormalityIndicator
        {
            Normal = 60
        };
        return new QuestLabResultReceivedHandler(A.Dummy<ILogger<QuestLabResultReceivedHandler>>(),
            _mediator,
            _transactionSupplier,
            A.Fake<IPublishObservability>(),
            _applicationTime,
            _featureFlags,
            _fakeMapper,
            _normalityIndicator,
            _billableRules
        );
    }

    [Fact]
    public async Task Handle_QuestLabResultDoesNotExist_DatabaseUpdated()
    {
        var e = new EgfrLabResult();
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send
                (A<QueryQuestLabResult>._, A<CancellationToken>._))
            .Returns((QuestLabResult)null);

        await subject.Handle(e, _fakeContext);

        A.CallTo(() => _mediator.Send(A<QueryQuestLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryExamByCenseoId>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddQuestLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustHaveHappened();
        A.CallTo(_fakeMapper).MustHaveHappened(2, Times.Exactly);
        A.CallTo(_mediator).MustHaveHappened(6, Times.Exactly);
        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_ExamDoesNotExist_ThrowException()
    {
        var e = new EgfrLabResult();

        var subject = CreateSubject();

        A.CallTo(() => _mediator.Send
                (A<QueryExamByCenseoId>._, A<CancellationToken>._))
            .Returns((Exam)null);

        A.CallTo(() => _mediator.Send
                (A<QueryQuestLabResult>._, A<CancellationToken>._))
            .Returns((QuestLabResult)null);

        await Assert.ThrowsAsync<ExamNotFoundException>(() => subject.Handle(e, _fakeContext));

        A.CallTo(() => _mediator.Send(A<QueryExamByCenseoId>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryQuestLabResult>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddQuestLabResult>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
        _transactionSupplier.AssertRollback();
    }

    [Fact]
    public async Task Handle_QuestLabResultDoesExist_DatabaseNotUpdated()
    {
        var kafkaQuestLabResultEvent = new EgfrLabResult
        {
            CenseoId = "abc", CollectionDate = DateTimeOffset.Now
        };
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send
                (A<QueryQuestLabResult>._, A<CancellationToken>._))
            .Returns(new QuestLabResult());

        await subject.Handle(kafkaQuestLabResultEvent, _fakeContext);

        A.CallTo(() => _mediator.Send(A<AddQuestLabResult>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(_fakeMapper).MustHaveHappened(1, Times.Exactly);
        A.CallTo(_mediator).MustHaveHappened(2, Times.Exactly);
        _transactionSupplier.AssertCommit();
    }

    [Theory]
    [InlineData(75, "Normal", "N")]
    [InlineData(60, "Normal", "N")]
    [InlineData(59, "Abnormal", "A")]
    [InlineData(0, "Undetermined", "U")]
    [InlineData(-5, "Undetermined", "U")]
    [InlineData(null, "Undetermined", "U")]
    public async Task Handle_QuestLabResult_Normality_Values(int? eGfrResult, string expectedNormality, string expectedNormalityCode)
    {
        var QuestLabResult = new QuestLabResult
        {
            eGFRResult = eGfrResult
        };

        var kafkaQuestLabResultEvent = new EgfrLabResult
        {
            eGFRResult = eGfrResult,
            CenseoId = "abc",
            CollectionDate = DateTimeOffset.Now
        };

        A.CallTo(() => _fakeMapper.Map<QuestLabResult>(A<EgfrLabResult>._))
            .Returns(QuestLabResult);
        A.CallTo(() => _mediator.Send
                (A<QueryQuestLabResult>._, A<CancellationToken>._))
            .Returns((QuestLabResult)null);

        var subject = CreateSubject();

        await subject.Handle(kafkaQuestLabResultEvent, _fakeContext);

        Assert.Equal(expectedNormality, QuestLabResult.Normality);
        Assert.Equal(eGfrResult, QuestLabResult.eGFRResult);
        Assert.Equal(expectedNormalityCode, QuestLabResult.NormalityCode);
        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_Payment_NotProcessed_When_PaymentFeature_Disabled()
    {
        var e = new EgfrLabResult();
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<QueryQuestLabResult>._, A<CancellationToken>._)).Returns((QuestLabResult)null);
        A.CallTo(() => _featureFlags.EnableProviderPayCdi).Returns(false);

        await subject.Handle(e, _fakeContext);

        A.CallTo(() => _mediator.Send(A<QueryQuestLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryExamByCenseoId>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddQuestLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryPayableCdiStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
        _transactionSupplier.AssertCommit();
    }

    [Theory]
    [MemberData(nameof(CdiStatusCollection))]
    public async Task Handle_Payment_Processed_When_PaymentFeature_Enabled_And_When_PayableCdiEvents_Received(ExamStatusCode statusCode, string eventName)
    {
        var e = new EgfrLabResult();
        var examStatus = A.Fake<ExamStatus>();
        examStatus.ExamStatusCode = statusCode;
        examStatus.ExamStatusCodeId = statusCode.StatusCodeId;
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<QueryQuestLabResult>._, A<CancellationToken>._)).Returns((QuestLabResult)null);
        A.CallTo(() => _featureFlags.EnableProviderPayCdi).Returns(true);
        A.CallTo(() => _mediator.Send(A<QueryPayableCdiStatus>._, A<CancellationToken>._)).Returns(examStatus);

        await subject.Handle(e, _fakeContext);

        A.CallTo(() => _mediator.Send(A<QueryQuestLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryExamByCenseoId>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddQuestLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryPayableCdiStatus>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _fakeMapper.Map<ProviderPayRequest>(A<Exam>._)).MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertCommit();
        Assert.NotNull(_fakeContext.SentMessages);
        var providerPayRequest = _fakeContext.FindSentMessage<ProviderPayRequest>();
        Assert.NotNull(providerPayRequest);
        Assert.Equal(providerPayRequest.ParentEvent, eventName);
    }

    public static IEnumerable<object[]> CdiStatusCollection()
    {
        yield return [ExamStatusCode.CdiPassedReceived, "CDIPassedEvent"];
        yield return [ExamStatusCode.CdiFailedWithPayReceived, "CDIFailedEvent"];
    }
}