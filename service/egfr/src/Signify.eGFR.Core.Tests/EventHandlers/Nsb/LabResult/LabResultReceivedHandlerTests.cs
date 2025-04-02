using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using EgfrNsbEvents;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.BusinessRules;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Models;
using Signify.eGFR.Core.Queries;
using Xunit;
using NormalityIndicator = Signify.eGFR.Core.Configs.NormalityIndicator;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.LabResult;

public class LabResultReceivedHandlerTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly FakeApplicationTime _applicationTime = new();
    private NormalityIndicator _normalityIndicator = A.Fake<NormalityIndicator>();
    private readonly TestableMessageHandlerContext _fakeContext = new();
    private readonly IBillableRules _billableRules = A.Fake<IBillableRules>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IFeatureFlags _featureFlags = A.Fake<IFeatureFlags>();

    private LabResultReceivedHandler CreateSubject()
    {
        _normalityIndicator = new NormalityIndicator
        {
            Normal = 60
        };
        return new LabResultReceivedHandler(A.Dummy<ILogger<LabResultReceivedHandler>>(), _mediator, _transactionSupplier,
            A.Fake<IPublishObservability>(), _applicationTime, _mapper, _normalityIndicator, _featureFlags,
            _billableRules);
    }

    [Fact]
    public async Task Handle_LabResultDoesNotExist_DatabaseUpdated()
    {
        var e = new KedEgfrLabResult();
        
        ResultsReceived resultsReceived = new ResultsReceived
        {
            EvaluationId = 123,
            Result = new Group
            {
                AbnormalIndicator = "N",
                Description = "Test",
                Result = 60.0m
            }
        };
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send
                (A<QueryLabResultByExamId>._, A<CancellationToken>._))
            .Returns<Data.Entities.LabResult>(null);
        
        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns<ExamNotPerformed>(null);
        A.CallTo(() => _mapper.Map<ResultsReceived>(A<Data.Entities.LabResult>._))
            .Returns(resultsReceived);
        
        await subject.Handle(e, _fakeContext);

        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryExam>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustHaveHappened();
        A.CallTo(_mapper).MustHaveHappened(2, Times.Exactly);
        A.CallTo(_mediator).MustHaveHappened(7, Times.Exactly);
    }

    [Fact]
    public async Task Handle_ExamDoesNotExist_ThrowException()
    {
        var e = new KedEgfrLabResult();

        var subject = CreateSubject();

        A.CallTo(() => _mediator.Send
                (A<QueryExam>._, A<CancellationToken>._))
            .Returns<Exam>(null);

        A.CallTo(() => _mediator.Send
                (A<QueryLabResultByExamId>._, A<CancellationToken>._))
            .Returns<Data.Entities.LabResult>(null);

        await Assert.ThrowsAsync<ExamNotFoundByEvaluationException>(() => subject.Handle(e, _fakeContext));

        A.CallTo(() => _mediator.Send(A<QueryExam>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_LabResultDoesExist_DatabaseNotUpdated()
    {
        var kafkaLabResultEvent = new KedEgfrLabResult
        {
            EvaluationId = 123
        };
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send
                (A<QueryLabResultByExamId>._, A<CancellationToken>._))
            .Returns(new Data.Entities.LabResult());

        await subject.Handle(kafkaLabResultEvent, _fakeContext);

        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(_mapper).MustHaveHappened(0, Times.Exactly);
        A.CallTo(_mediator).MustHaveHappened(2, Times.Exactly);
    }

    [Theory]
    [InlineData(75.01, 2)]
    [InlineData(60.15, 2)]
    [InlineData(59.18, 3)]
    [InlineData(0.0, 1)]
    [InlineData(-5.1, 1)]
    public async Task Handle_LabResult_Normality_Values(decimal eGfrResult, int expectedNormalityId)
    {
        var labResult = new Data.Entities.LabResult
        {
            EgfrResult = eGfrResult
        };

        var kafkaLabResultEvent = new KedEgfrLabResult
        {
            EgfrResult = eGfrResult,
            EvaluationId = 123,
        };
        
        ResultsReceived resultsReceived = new ResultsReceived
        {
            EvaluationId = 123,
            Result = new Group
            {
                AbnormalIndicator = "N",
                Description = "Test",
                Result = 60.0m
            }
        };

        A.CallTo(() => _mapper.Map<Data.Entities.LabResult>(A<KedEgfrLabResult>._))
            .Returns(labResult);
        A.CallTo(() => _mediator.Send
                (A<QueryLabResultByExamId>._, A<CancellationToken>._))
            .Returns((Data.Entities.LabResult)null);
        A.CallTo(() => _mapper.Map<ResultsReceived>(A<Data.Entities.LabResult>._))
            .Returns(resultsReceived);

        var subject = CreateSubject();

        await subject.Handle(kafkaLabResultEvent, _fakeContext);

        Assert.Equal(expectedNormalityId, labResult.NormalityIndicatorId);
        Assert.Equal(eGfrResult, labResult.EgfrResult);
        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_Payment_NotProcessed_When_PaymentFeature_Disabled()
    {
        var e = new KedEgfrLabResult();
        ResultsReceived resultsReceived = new ResultsReceived
        {
            EvaluationId = 123,
            Result = new Group
            {
                AbnormalIndicator = "N",
                Description = "Test",
                Result = 60.0m
            }
        };
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._)).Returns((Data.Entities.LabResult)null);
        A.CallTo(() => _featureFlags.EnableProviderPayCdi).Returns(false);

        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns<ExamNotPerformed>(null);
        A.CallTo(() => _mapper.Map<ResultsReceived>(A<Data.Entities.LabResult>._))
            .Returns(resultsReceived);
        
        await subject.Handle(e, _fakeContext);

        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryExam>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustHaveHappened();
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
        var e = new KedEgfrLabResult();
        ResultsReceived resultsReceived = new ResultsReceived
        {
            EvaluationId = 123,
            Result = new Group
            {
                AbnormalIndicator = "N",
                Description = "Test",
                Result = 60.0m
            }
        };
        var examStatus = A.Fake<ExamStatus>();
        examStatus.ExamStatusCode = statusCode;
        examStatus.ExamStatusCodeId = statusCode.StatusCodeId;
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._)).Returns((Data.Entities.LabResult)null);
        A.CallTo(() => _featureFlags.EnableProviderPayCdi).Returns(true);
        A.CallTo(() => _mediator.Send(A<QueryPayableCdiStatus>._, A<CancellationToken>._)).Returns(examStatus);
        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns<ExamNotPerformed>(null);
        A.CallTo(() => _mapper.Map<ResultsReceived>(A<Data.Entities.LabResult>._))
            .Returns(resultsReceived);
        
        await subject.Handle(e, _fakeContext);

        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryExam>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryPayableCdiStatus>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mapper.Map<ProviderPayRequest>(A<Exam>._)).MustHaveHappenedOnceExactly();
        _transactionSupplier.AssertCommit();
        Assert.NotNull(_fakeContext.SentMessages);
        var providerPayRequest = _fakeContext.FindSentMessage<ProviderPayRequest>();
        Assert.NotNull(providerPayRequest);
        Assert.Equal(providerPayRequest.ParentEvent, eventName);
    }

    
    [Fact]
    public async Task Handle_LabResultDoesNotExistAndExamNotPerformed_DatabaseUpdated()
    {
        var e = new KedEgfrLabResult();
        ResultsReceived resultsReceived = new ResultsReceived
        {
            EvaluationId = 123,
            Result = new Group
            {
                AbnormalIndicator = "N",
                Description = "Test",
                Result = 60.0m
            }
        };
        A.CallTo(() => _mapper.Map<ResultsReceived>(A<Data.Entities.LabResult>._))
            .Returns(resultsReceived);
        
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send
                (A<QueryLabResultByExamId>._, A<CancellationToken>._))
            .Returns((Data.Entities.LabResult)null);
        
        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns(new ExamNotPerformed());
        
        
        await subject.Handle(e, _fakeContext);

        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryExam>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<ProcessBillingEvent>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(_mapper).MustHaveHappened(2, Times.Exactly);
        A.CallTo(_mediator).MustHaveHappened(5, Times.Exactly);
    }
    
    public static IEnumerable<object[]> CdiStatusCollection()
    {
        yield return [ExamStatusCode.CdiPassedReceived, "CDIPassedEvent"];
        yield return [ExamStatusCode.CdiFailedWithPayReceived, "CDIFailedEvent"];
    }
    
    [Fact]
    public async Task Handle_LabResultDoesNotExist_DatabaseUpdated_And_Billing_Happens()
    {
        var e = new KedEgfrLabResult();
        ResultsReceived resultsReceived = new ResultsReceived
        {
            EvaluationId = 123,
            Result = new Group
            {
                AbnormalIndicator = "N",
                Description = "Test",
                Result = 60.0m
            }
        };
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send
                (A<QueryLabResultByExamId>._, A<CancellationToken>._))
            .Returns((Data.Entities.LabResult)null);
        
        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns((ExamNotPerformed)null);
        
        A.CallTo(() => _featureFlags.EnableDirectBilling).Returns(false);
        A.CallTo(() => _mapper.Map<ResultsReceived>(A<Data.Entities.LabResult>._))
            .Returns(resultsReceived);
        
        await subject.Handle(e, _fakeContext);

        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryExam>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustHaveHappened();
        A.CallTo(_mapper).MustHaveHappened(2, Times.Exactly);
        A.CallTo(_mediator).MustHaveHappened(7, Times.Exactly);
        
        Assert.Single(_fakeContext.SentMessages);
        
        var processBillingEvent = _fakeContext.FindSentMessage<ProcessBillingEvent>();
        Assert.Equal(ProductCodes.eGFR_RcmBilling, processBillingEvent.RcmProductCode);
    }

    [Fact]
    public async Task Handle_LabResultDoesNotExist_DatabaseUpdated_And_Direct_Billing_Happens()
    {
        var e = new KedEgfrLabResult();
        ResultsReceived resultsReceived = new ResultsReceived
        {
            EvaluationId = 123,
            Result = new Group
            {
                AbnormalIndicator = "N",
                Description = "Test",
                Result = 60.0m
            }
        };
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send
                (A<QueryLabResultByExamId>._, A<CancellationToken>._))
            .Returns<Data.Entities.LabResult>(null);
        
        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns<ExamNotPerformed>(null);
        
        A.CallTo(() => _featureFlags.EnableDirectBilling).Returns(true);
        A.CallTo(() => _mapper.Map<ResultsReceived>(A<Data.Entities.LabResult>._))
            .Returns(resultsReceived);
        
        await subject.Handle(e, _fakeContext);

        A.CallTo(() => _mediator.Send(A<QueryLabResultByExamId>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<QueryExam>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustHaveHappened();
        A.CallTo(_mapper).MustHaveHappened(2, Times.Exactly);
        A.CallTo(_mediator).MustHaveHappened(7, Times.Exactly);
        
        Assert.Single(_fakeContext.SentMessages);
        
        var processBillingEvent = _fakeContext.FindSentMessage<ProcessBillingEvent>();
        Assert.Equal(ProductCodes.EGfrRcmBillingResults, processBillingEvent.RcmProductCode);
    }
}