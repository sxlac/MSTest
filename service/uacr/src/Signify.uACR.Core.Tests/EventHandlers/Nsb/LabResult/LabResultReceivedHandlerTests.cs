using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.ApiClients.EvaluationApi.Responses;
using Signify.uACR.Core.BusinessRules;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Configs;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.EventHandlers.Nsb;
using Signify.uACR.Core.Events;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.FeatureFlagging;
using Signify.uACR.Core.Infrastructure;
using Signify.uACR.Core.Models;
using Signify.uACR.Core.Queries;
using UacrNsbEvents;
using Xunit;
using PdfEntity = Signify.uACR.Core.Data.Entities.PdfDeliveredToClient;

namespace Signify.uACR.Core.Tests.EventHandlers.Nsb;

public class LabResultReceivedHandlerTests
{
    private readonly IMapper _fakeMapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IApplicationTime _applicationTime = A.Fake<IApplicationTime>();
    private readonly TestableMessageHandlerContext _fakeContext = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private NormalityIndicator _normalityIndicator = A.Fake<NormalityIndicator>();
    private readonly IBillableRules _billableRules = A.Fake<IBillableRules>();
    private readonly IFeatureFlags _featureFlags = A.Fake<IFeatureFlags>();

    private LabResultReceivedHandler CreateSubject()
    {
        _normalityIndicator = new NormalityIndicator
        {
            Normal = 30
        };

        return new LabResultReceivedHandler(A.Dummy<ILogger<LabResultReceivedHandler>>(), _fakeMapper, _mediator, _applicationTime,
            A.Fake<ITransactionSupplier>(), _publishObservability, _normalityIndicator, _featureFlags, _billableRules);
    }

    [Fact]
    public async Task Handle_ExamDoesNotExist_ThrowsException()
    {
        var labResult = new KedUacrLabResult();
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send
                (A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(null as Exam);

        var evaluationProductCodes = new EvaluationProductCodes();
        
        evaluationProductCodes.ProductCodes.Add("UACR");
        evaluationProductCodes.ProductCodes.Add("EGFR");
        
        A.CallTo(() => _mediator.Send
                (A<QueryEvaluationProductCodes>._, A<CancellationToken>._))
            .Returns(evaluationProductCodes.ProductCodes);
        
        await Assert.ThrowsAsync<ExamNotFoundException>(async () => await subject.Handle(labResult, _fakeContext));

        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.LabResult.LabResultExamDoesNotExist), true)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_ProductCodeMissing_ThrowsException()
    {
        var labResult = new KedUacrLabResult();
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send
                (A<QueryExamByEvaluation>._, A<CancellationToken>._))
            .Returns(null as Exam);

        var evaluationProductCodes = new EvaluationProductCodes();
        
        evaluationProductCodes.ProductCodes.Add("EGFR");
        
        A.CallTo(() => _mediator.Send
                (A<QueryEvaluationProductCodes>._, A<CancellationToken>._))
            .Returns(evaluationProductCodes.ProductCodes);
        
        await Assert.ThrowsAsync<KedProductNotFoundException>(async () => await subject.Handle(labResult, _fakeContext));

        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _publishObservability.RegisterEvent(
            A<ObservabilityEvent>.That.Matches(e => e.EventType == Observability.LabResult.LabResultReceivedButProductCodeMissing), true)).MustHaveHappenedOnceExactly();
    }
    
    [Fact]
    public async Task Handle_LabResultDoesNotExist_DatabaseUpdated()
    {
        var labResult = new KedUacrLabResult();
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send
                (A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .Returns((LabResult)null);
        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns((ExamNotPerformed)null);
        
        await subject.Handle(labResult, _fakeContext);

        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(_fakeMapper).MustHaveHappened(1, Times.Exactly);
        A.CallTo(_mediator).MustHaveHappened(6, Times.Exactly);
    }

    [Fact]
    public async Task Handle_LabResultDoesExist_DatabaseNotUpdated()
    {
        var kafkaLabResultEvent = new KedUacrLabResult
        {
            EvaluationId = 12345,
            DateLabReceived = DateTimeOffset.Now
        };
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send
                (A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .Returns(new LabResult());

        await subject.Handle(kafkaLabResultEvent, _fakeContext);

        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(_mediator).MustHaveHappened(2, Times.Exactly);
    }

    [Theory]
    [InlineData(10, "Normal", "N", true)]
    [InlineData(20, "Normal", "N", true)]
    [InlineData(30, "Abnormal", "A", true)]
    [InlineData(80, "Abnormal", "A", true)]
    [InlineData(0, "Undetermined", "U", false)]
    [InlineData(-5, "Undetermined", "U", false)]
    [InlineData(null, "Undetermined", "U", false)]
    public async Task Handle_LabResult_Normality_Values_With_Billing(int? uacrResult, string expectedNormality, string expectedNormalityCode, bool isBillable)
    {
        var labResult = new LabResult
        {
            UacrResult = uacrResult
        };

        var kafkaLabResultEvent = new KedUacrLabResult
        {
            UacrResult = uacrResult,
            EvaluationId = 12345,
            DateLabReceived = DateTimeOffset.Now
        };

        A.CallTo(() => _fakeMapper.Map<LabResult>(A<KedUacrLabResult>._))
            .Returns(labResult);
        A.CallTo(() => _mediator.Send
                (A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .Returns((LabResult)null);
        
        A.CallTo(() => _featureFlags.EnableDirectBilling).Returns(false);
        
        A.CallTo(() => _featureFlags.EnableBilling).Returns(true);
        
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new QueryPdfDeliveredToClientResult(new PdfEntity()));
        var status = A.Fake<BusinessRuleStatus>();
        status.IsMet = isBillable;
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(status);

        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns((ExamNotPerformed)null);
        
        var subject = CreateSubject();

        await subject.Handle(kafkaLabResultEvent, _fakeContext);

        Assert.Equal(expectedNormality, labResult.Normality);
        Assert.Equal(uacrResult, labResult.UacrResult);
        Assert.Equal(expectedNormalityCode, labResult.NormalityCode);

        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._))
            .MustHaveHappened();
        Assert.Single(_fakeContext.SentMessages);
        var processBillingEvent = _fakeContext.FindSentMessage<ProcessBillingEvent>();
        Assert.Equal(isBillable, processBillingEvent.IsBillable);
        
        Assert.Equal(ProductCodes.uACR_RcmBilling, processBillingEvent.RcmProductCode);
    }
    
    [Fact]
    public async Task Handle_LabResultDoesNotExistAndExamNotPerformed_DatabaseUpdated()
    {
        var labResult = new KedUacrLabResult();
        var subject = CreateSubject();
        A.CallTo(() => _mediator.Send
                (A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .Returns((LabResult)null);
        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns(new ExamNotPerformed());
        
        await subject.Handle(labResult, _fakeContext);

        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddLabResult>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddExamStatus>._, A<CancellationToken>._)).MustHaveHappened();
        
        A.CallTo(() => _mediator.Send(A<PublishResults>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<ProcessBillingEvent>._, A<CancellationToken>._)).MustNotHaveHappened();
        
        A.CallTo(_fakeMapper).MustHaveHappened(1, Times.Exactly);
        A.CallTo(_mediator).MustHaveHappened(5, Times.Exactly);
    }
    
     [Theory]
    [InlineData(10, "Normal", "N", true)]
    [InlineData(20, "Normal", "N", true)]
    [InlineData(30, "Abnormal", "A", true)]
    [InlineData(80, "Abnormal", "A", true)]
    [InlineData(0, "Undetermined", "U", false)]
    [InlineData(-5, "Undetermined", "U", false)]
    [InlineData(null, "Undetermined", "U", false)]
    public async Task Handle_LabResult_Normality_Values_With_Direct_Billing(int? uacrResult, string expectedNormality, string expectedNormalityCode, bool isBillable)
    {
        var labResult = new LabResult
        {
            UacrResult = uacrResult
        };

        var kafkaLabResultEvent = new KedUacrLabResult
        {
            UacrResult = uacrResult,
            EvaluationId = 12345,
            DateLabReceived = DateTimeOffset.Now
        };

        A.CallTo(() => _fakeMapper.Map<LabResult>(A<KedUacrLabResult>._))
            .Returns(labResult);
        A.CallTo(() => _mediator.Send
                (A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .Returns((LabResult)null);
        
        A.CallTo(() => _featureFlags.EnableDirectBilling).Returns(true);
        
        A.CallTo(() => _featureFlags.EnableBilling).Returns(true);
        
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
            .Returns(new QueryPdfDeliveredToClientResult(new PdfEntity()));
        var status = A.Fake<BusinessRuleStatus>();
        status.IsMet = isBillable;
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._)).Returns(status);

        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns((ExamNotPerformed)null);
        
        var subject = CreateSubject();

        await subject.Handle(kafkaLabResultEvent, _fakeContext);

        Assert.Equal(expectedNormality, labResult.Normality);
        Assert.Equal(uacrResult, labResult.UacrResult);
        Assert.Equal(expectedNormalityCode, labResult.NormalityCode);

        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _billableRules.IsBillable(A<BillableRuleAnswers>._))
            .MustHaveHappened();
        Assert.Single(_fakeContext.SentMessages);
        var processBillingEvent = _fakeContext.FindSentMessage<ProcessBillingEvent>();
        Assert.Equal(isBillable, processBillingEvent.IsBillable);
        
        Assert.Equal(ProductCodes.UAcrRcmBillingResults, processBillingEvent.RcmProductCode);
    }
}