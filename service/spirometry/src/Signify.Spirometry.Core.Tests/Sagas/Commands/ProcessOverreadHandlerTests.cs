using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Queries;
using Signify.Spirometry.Core.Services;
using SpiroNsb.SagaCommands;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

using NormalityIndicator = Signify.Spirometry.Core.Data.Entities.NormalityIndicator;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;

namespace Signify.Spirometry.Core.Tests.Sagas.Commands;

public class ProcessOverreadHandlerTests
{
    private const int EvaluationId = 99; // Value doesn't matter, just something that's unlikely to conflict with other test data
    private const int AppointmentId = 98;

    private readonly DateTime _overreadReceivedDateTime = DateTime.UtcNow;

    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly IGetLoopbackConfig _getConfig = A.Fake<IGetLoopbackConfig>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IExamQualityService _examQualityService = A.Fake<IExamQualityService>();

    private readonly IBufferedTransaction _transaction = A.Fake<IBufferedTransaction>();

    private ProcessOverreadHandler CreateSubject(bool enable = true)
    {
        A.CallTo(() => _getConfig.ShouldProcessOverreads)
            .Returns(enable);

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(_transaction);

        return new ProcessOverreadHandler(A.Dummy<ILogger<ProcessOverreadHandler>>(), _applicationTime, _getConfig,
            _transactionSupplier, _mediator, _mapper, _examQualityService);
    }

    [Fact]
    public async Task Handle_WhenDisabled_DoesNothing()
    {
        // Arrange
        var request = new ProcessOverread(default, default);

        var context = new TestableMessageHandlerContext();

        // Act
        await Assert.ThrowsAnyAsync<FeatureDisabledException>(async () => await CreateSubject(false).Handle(request, context));

        // Assert
        A.CallTo(() => _getConfig.ShouldProcessOverreads)
            .MustHaveHappened();

        A.CallTo(_transactionSupplier)
            .MustNotHaveHappened();
        A.CallTo(_mediator)
            .MustNotHaveHappened();

        Assert.Empty(context.SentMessages);
        Assert.Empty(context.PublishedMessages);
    }

    private void Setup(NormalityIndicator overreadNormality, decimal overreadRatio, bool needsFlag = true)
    {
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._))
            .Returns(new SpirometryExam
            {
                EvaluationId = EvaluationId,
                AppointmentId = AppointmentId,
                SpirometryExamResult = new SpirometryExamResult()
            });

        A.CallTo(() => _mediator.Send(A<QueryOverreadResult>._, A<CancellationToken>._))
            .Returns(new OverreadResult
            {
                NormalityIndicatorId = (overreadNormality ?? NormalityIndicator.Normal).NormalityIndicatorId, // if `default` is passed, the test doesn't care about the normality
                Fev1FvcRatio = overreadRatio,
                ReceivedDateTime = _overreadReceivedDateTime
            });

        A.CallTo(() => _examQualityService.NeedsOverread(A<SpirometryExamResult>._))
            .Returns(true);

        A.CallTo(() => _examQualityService.NeedsFlag(A<SpirometryExamResult>._))
            .Returns(needsFlag);
    }

    [Fact]
    public async Task Handle_WhenOverreadAlreadyProcessed_DoesNothing()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._))
            .Returns(new SpirometryExam
            {
                EvaluationId = EvaluationId,
                AppointmentId = AppointmentId,
                SpirometryExamResult = new SpirometryExamResult
                {
                    OverreadFev1FvcRatio = 1m // Any non-null value
                }
            });

        var context = new TestableMessageHandlerContext();

        var request = new ProcessOverread(EvaluationId, default);

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>.That.Matches(q =>
                    q.EvaluationId == EvaluationId &&
                    q.IncludeResults),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<QueryOverreadResult>.That.Matches(q =>
                    q.AppointmentId == AppointmentId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(_transactionSupplier)
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<UpdateExamResults>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        Assert.Empty(context.SentMessages);
        Assert.Empty(context.PublishedMessages);
    }

    [Fact]
    public async Task Handle_WhenOverreadNotRequired_DoesNothing()
    {
        // Arrange
        Setup(default, default);

        A.CallTo(() => _examQualityService.NeedsOverread(A<SpirometryExamResult>._))
            .Returns(false);

        var context = new TestableMessageHandlerContext();

        var request = new ProcessOverread(EvaluationId, default);

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(_transactionSupplier)
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<UpdateExamResults>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        Assert.Empty(context.SentMessages);
        Assert.Empty(context.PublishedMessages);
    }

    [Theory]
    [MemberData(nameof(Handle_WhenEnabledAndNotProcessed_UpdatesExamResultsFromOverread_TestData))]
    public async Task Handle_WhenEnabledAndNotProcessed_UpdatesExamResultsFromOverread_Tests(NormalityIndicator overreadNormality)
    {
        // Arrange
        const decimal overreadRatio = decimal.One;

        Setup(overreadNormality, overreadRatio);

        var request = new ProcessOverread(EvaluationId, default);

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<UpdateExamResults>.That.Matches(u =>
                    u.Results.NormalityIndicatorId == overreadNormality.NormalityIndicatorId &&
                    u.Results.OverreadFev1FvcRatio == overreadRatio &&
                    !u.Results.Fev1FvcRatio.HasValue), // Ensure we're not updating the POC ratio
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _transaction.Dispose())
            .MustHaveHappened();
    }

    public static IEnumerable<object[]> Handle_WhenEnabledAndNotProcessed_UpdatesExamResultsFromOverread_TestData()
    {
        yield return
        [
            NormalityIndicator.Normal
        ];

        yield return
        [
            NormalityIndicator.Abnormal
        ];

        yield return
        [
            NormalityIndicator.Undetermined
        ];
    }

    [Fact]
    public async Task Handle_WhenEnabledAndNotProcessed_SendsStatusEvents()
    {
        // Arrange
        Setup(default, default);

        var overreadReceivedDateTime = DateTime.UtcNow;

        A.CallTo(() => _mediator.Send(A<QueryOverreadResult>._, A<CancellationToken>._))
            .Returns(new OverreadResult
            {
                ReceivedDateTime = overreadReceivedDateTime
            });

        var request = new ProcessOverread(EvaluationId, default);

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>.That.Matches(e =>
                    e.Exam != null &&
                    e.StatusCode == StatusCode.OverreadProcessed &&
                    e.StatusDateTime == _applicationTime.UtcNow()),
                A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>.That.Matches(e =>
                    e.Exam != null &&
                    e.StatusCode == StatusCode.ResultsReceived &&
                    e.StatusDateTime == overreadReceivedDateTime),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_WhenEnabledAndNotProcessed_PublishesOverreadProcessedEvent_IsBillableTests(bool isBillable)
    {
        // Arrange
        Setup(default, default);

        A.CallTo(() => _mediator.Send(A<QueryBillability>._, A<CancellationToken>._))
            .Returns(new QueryBillabilityResult(isBillable));
        var request = new ProcessOverread(EvaluationId, default);

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillability>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<SendOverreadProcessedEvent>.That.Matches(e =>
            e.OverreadProcessedBaseEvent.EvaluationId == EvaluationId
            && e.OverreadProcessedBaseEvent.CreatedDateTime == _applicationTime.UtcNow()
            && e.OverreadProcessedBaseEvent.IsBillable == isBillable
        ), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData( false)]
    [InlineData(true)]
    public async Task Handle_WhenEnabledAndNotProcessed_PublishesOverreadProcessedEvent_IsPayableTests( bool isPayable)
    {
        // Arrange
        Setup(default, default);

        A.CallTo(() => _mediator.Send(A<QueryBillability>._, A<CancellationToken>._))
            .Returns(new QueryBillabilityResult(true));
        A.CallTo(() => _mediator.Send(A<QueryPayable>._, A<CancellationToken>._))
            .Returns(new QueryPayableResult(isPayable));
        var request = new ProcessOverread(EvaluationId, default);

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillability>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<SendOverreadProcessedEvent>.That.Matches(e =>
            e.OverreadProcessedBaseEvent.EvaluationId == EvaluationId
            && e.OverreadProcessedBaseEvent.CreatedDateTime == _applicationTime.UtcNow()
            && e.IsPayable == isPayable
        ), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
    
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_WhenEnabledAndNotProcessed_PublishesResults_IsBillableTests(bool isBillable)
    {
        // Arrange
        Setup(default, default);

        A.CallTo(() => _mediator.Send(A<QueryBillability>._, A<CancellationToken>._))
            .Returns(new QueryBillabilityResult(isBillable));

        var request = new ProcessOverread(EvaluationId, default);

        // Act
        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        // Assert
        A.CallTo(() => _mediator.Send(A<PublishResults>.That.Matches(p =>
                    p.Event.IsBillable == isBillable),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_WhenEnabledAndNotProcessed_PublishesResults_NeedsFlag_WithProviderPayTests(bool needsFlag)
    {
        // Arrange
        Setup(default, default, needsFlag);

        var request = new ProcessOverread(EvaluationId, default);

        var context = new TestableMessageHandlerContext();
            
        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillability>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<SendOverreadProcessedEvent>.That.Matches(e =>
            e.OverreadProcessedBaseEvent.EvaluationId == EvaluationId
            && e.OverreadProcessedBaseEvent.NeedsFlag == needsFlag
        ), A<CancellationToken>._)).MustHaveHappenedOnceExactly();
            
    }

    [Fact]
    public async Task Handle_WhenEnabledAndNotProcessed_PublishesResults_ReceivedDateTest()
    {
        // Arrange 
        Setup(default, default);

        var request = new ProcessOverread(EvaluationId, default);

        // Act
        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        // Assert
        A.CallTo(() => _mediator.Send(A<PublishResults>.That.Matches(p =>
                    p.Event.ReceivedDate == _overreadReceivedDateTime),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    #region Sociable Unit Tests

    [Fact]
    public async Task Handle_WhenEnabledAndNotProcessed_DoesNotCallNeedsFlag_UnlessOverreadRatioSet_Test()
    {
        // Arrange
        Setup(default, default);

        var request = new ProcessOverread(default, default);

        var context = new TestableMessageHandlerContext();

        #region test the test

        // Ensure after this setup, the subject would call `NeedsFlag`
        await CreateSubject().Handle(request, context);

        A.CallTo(() => _examQualityService.NeedsFlag(A<SpirometryExamResult>._))
            .MustHaveHappened();

        #endregion test the test

        var realQualityService = new ExamQualityService(A.Dummy<ILogger<ExamQualityService>>(), A.Dummy<IGetLoopbackConfig>());

        var subject = new ProcessOverreadHandler(A.Dummy<ILogger<ProcessOverreadHandler>>(), _applicationTime, _getConfig,
            _transactionSupplier, _mediator, _mapper, realQualityService);

        // Act
        try
        {
            await subject.Handle(request, context);

            // Assert
            Assert.True(true); // We should get here

            // All this test does is ensures an `InvalidOperationException` is not thrown
            // by the `ExamQualityService`, which would mean that the handler did not set
            // the overread ratio before calling it
        }
        catch (InvalidOperationException ex)
        {
            Assert.Fail(ex.Message);
        }
    }

    #endregion Sociable Unit Tests
}