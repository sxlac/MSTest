using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Events.Status;
using Signify.Spirometry.Core.Queries;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Xunit;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;
using BillRequestSent = Signify.Spirometry.Core.Data.Entities.BillRequestSent;

namespace Signify.Spirometry.Core.Tests.Events
{
    public class ExamStatusHandlerTests
    {
        private readonly IMapper _mapper = A.Fake<IMapper>();
        private readonly IMediator _mediator = A.Fake<IMediator>();
        private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

        private ExamStatusEventHandler CreateSubject()
            => new ExamStatusEventHandler(A.Dummy<ILogger<ExamStatusEventHandler>>(), _mapper, _mediator, _publishObservability);

        [Theory]
        [InlineData(StatusCode.CdiPassedReceived, true, true)]
        [InlineData(StatusCode.CdiFailedWithPayReceived, true, true)]
        [InlineData(StatusCode.CdiFailedWithoutPayReceived, true, true)]
        [InlineData(StatusCode.ProviderPayableEventReceived, true, true)]
        [InlineData(StatusCode.ProviderNonPayableEventReceived, true, true)]
        [InlineData(StatusCode.SpirometryExamPerformed, false, false)]
        [InlineData(StatusCode.SpirometryExamNotPerformed, false, false)]
        [InlineData(StatusCode.BillableEventReceived, false, false)]
        [InlineData(StatusCode.BillRequestSent, false, false)]
        [InlineData(StatusCode.ClientPdfDelivered, false, false)]
        [InlineData(StatusCode.BillRequestNotSent, false, false)]
        [InlineData(StatusCode.OverreadProcessed, false, false)]
        [InlineData(StatusCode.ResultsReceived, false, false)]
        [InlineData(StatusCode.ClarificationFlagCreated, false, false)]
        [InlineData(StatusCode.ProviderPayRequestSent, false, true)]
        public async Task Handle_WithMessage_AddsExamStatus(StatusCode statusCode, bool alwaysAdd, bool publishObservability)
        {
            const int evaluationId = 1;
            const int spirometryExamId = 2;

            var request = new ExamStatusEvent
            {
                EventId = Guid.NewGuid(),
                Exam = new SpirometryExam
                {
                    EvaluationId = evaluationId,
                    SpirometryExamId = spirometryExamId
                },
                StatusCode = statusCode
            };

            var status = new ExamStatus
            {
                SpirometryExamId = spirometryExamId
            };

            A.CallTo(() => _mapper.Map<ExamStatus>(A<ExamStatusEvent>._))
                .Returns(status);

            await CreateSubject().Handle(request, default);

            A.CallTo(() => _mediator.Send(
                    A<AddExamStatus>.That.Matches(a =>
                        a.EventId == request.EventId && a.EvaluationId == evaluationId && a.Status == status && a.AlwaysAddStatus == alwaysAdd),
                    A<CancellationToken>._))
                .MustHaveHappened();
            A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, false))
                .MustHaveHappened(publishObservability ? 1 : 0, Times.Exactly);
        }

        [Theory]
        [InlineData(StatusCode.SpirometryExamPerformed)]
        [InlineData(StatusCode.SpirometryExamNotPerformed)]
        [InlineData(StatusCode.BillRequestSent)]
        [InlineData(StatusCode.BillRequestNotSent)]
        [InlineData(StatusCode.ResultsReceived)]
        [InlineData(StatusCode.ProviderPayRequestSent)]
        [InlineData(StatusCode.ClarificationFlagCreated)]
        public async Task Handle_WithPublishableStatus_PublishesStatusEventToKafka(StatusCode statusCode)
        {
            //Setup
            const int evaluationId = 1;
            const int spirometryExamId = 2;

            var request = new ExamStatusEvent
            {
                EventId = Guid.NewGuid(),
                Exam = new SpirometryExam
                {
                    EvaluationId = evaluationId,
                    SpirometryExamId = spirometryExamId
                },
                StatusCode = statusCode
            };

            var status = new ExamStatus { SpirometryExamId = spirometryExamId };

            A.CallTo(() => _mapper.Map<ExamStatus>(A<ExamStatusEvent>._))
                .Returns(status);

            A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
                .Returns(new ExamNotPerformed
                {
                    SpirometryExamId = spirometryExamId,
                    NotPerformedReasonId = 1,
                    NotPerformedReason = new NotPerformedReason(default, default, default)
                });

            A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>._, A<CancellationToken>._))
                .Returns(new QueryPdfDeliveredToClientResult(new PdfDeliveredToClient()));

            A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._))
                .Returns(new QueryBillRequestSentResult(new BillRequestSent()));

            //Act
            await CreateSubject().Handle(request, default);

            //Assert
            switch (statusCode)
            {
                case StatusCode.SpirometryExamPerformed:
                    A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(s =>
                            s.Status is Performed), A<CancellationToken>._))
                        .MustHaveHappenedOnceExactly();
                    break;
                case StatusCode.SpirometryExamNotPerformed:
                    A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>.That.Matches(q =>
                            q.EvaluationId == evaluationId), A<CancellationToken>._))
                        .MustHaveHappenedOnceExactly();
                    A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                            p.Status is NotPerformed), A<CancellationToken>._))
                        .MustHaveHappenedOnceExactly();
                    break;
                case StatusCode.BillRequestSent:
                    A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>.That.Matches(q =>
                            q.EvaluationId == evaluationId), A<CancellationToken>._))
                        .MustHaveHappenedOnceExactly();
                    A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                            p.Status is Signify.Spirometry.Core.Events.Status.BillRequestSent), A<CancellationToken>._))
                        .MustHaveHappenedOnceExactly();
                    break;
                case StatusCode.BillRequestNotSent:
                    A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>.That.Matches(q =>
                            q.EvaluationId == evaluationId), A<CancellationToken>._))
                        .MustHaveHappened();
                    A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                            p.Status is BillRequestNotSent), A<CancellationToken>._))
                        .MustHaveHappenedOnceExactly();
                    break;
                case StatusCode.ResultsReceived:
                    A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                            p.Status is ResultsReceived), A<CancellationToken>._))
                        .MustHaveHappenedOnceExactly();
                    break;
                case StatusCode.ProviderPayRequestSent:
                    A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                            p.Status.GetType().Name.Contains(statusCode.ToString())), A<CancellationToken>._))
                        .MustHaveHappenedOnceExactly();
                    break;
                case StatusCode.ClarificationFlagCreated:
                    A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(s =>
                            s.Status is FlaggedForLoopback), A<CancellationToken>._))
                        .MustHaveHappenedOnceExactly();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "Status code passed into test has no assertions");
            }
        }

        [Theory]
        [InlineData(StatusCode.BillableEventReceived)]
        [InlineData(StatusCode.ClientPdfDelivered)]
        [InlineData(StatusCode.OverreadProcessed)]
        [InlineData(StatusCode.CdiPassedReceived)]
        [InlineData(StatusCode.CdiFailedWithPayReceived)]
        [InlineData(StatusCode.CdiFailedWithoutPayReceived)]
        public async Task Handle_WithoutPublishableStatus_DoesNotPublishStatusEventToKafka(StatusCode statusCode)
        {
            // Arrange
            const int evaluationId = 1;
            const int spirometryExamId = 2;

            var request = new ExamStatusEvent
            {
                EventId = Guid.NewGuid(),
                Exam = new SpirometryExam
                {
                    EvaluationId = evaluationId,
                    SpirometryExamId = spirometryExamId
                },
                StatusCode = statusCode
            };

            // Act
            await CreateSubject().Handle(request, default);

            // Assert
            A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        [Theory]
        [MemberData(nameof(Handle_WhenNotPerformed_NotPerformedReasonTypeTestData))]
        public async Task Handle_WhenNotPerformed_NotPerformedReasonTypeTests(int notPerformedAnswerId, string expectedReasonType)
        {
            // Arrange
            const int evaluationId = 1;
            const int spirometryExamId = 2;

            var request = new ExamStatusEvent
            {
                EventId = Guid.NewGuid(),
                Exam = new SpirometryExam
                {
                    EvaluationId = evaluationId,
                    SpirometryExamId = spirometryExamId
                },
                StatusCode = StatusCode.SpirometryExamNotPerformed
            };

            A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
                .Returns(new ExamNotPerformed
                {
                    SpirometryExamId = spirometryExamId,
                    NotPerformedReason = new NotPerformedReason(default, notPerformedAnswerId, default)
                });

            // Act
            await CreateSubject().Handle(request, default);

            // Assert
            A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                        ((NotPerformed)p.Status).ReasonType == expectedReasonType),
                    A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }

        public static IEnumerable<object[]> Handle_WhenNotPerformed_NotPerformedReasonTypeTestData()
        {
            yield return new object[] { 50923, "Member refused" }; // Member recently completed
            yield return new object[] { 50924, "Member refused" }; // Scheduled to complete
            yield return new object[] { 50925, "Member refused" }; // Member apprehension
            yield return new object[] { 50926, "Member refused" }; // Not interested

            yield return new object[] { 50928, "Unable to perform" }; // Technical issue
            yield return new object[] { 50929, "Unable to perform" }; // Environmental issue
            yield return new object[] { 50930, "Unable to perform" }; // No supplies or equipment
            yield return new object[] { 50931, "Unable to perform" }; // Insufficient training
            yield return new object[] { 50932, "Unable to perform" }; // Member physically unable
            yield return new object[] { 51960, "Unable to perform" }; // Member outside demographic ranges

            yield return new object[] { 1, string.Empty }; // Invalid/unknown answer id
        }

        [Fact]
        public async Task Handle_WhenDeterminingWhetherToPublishToKafka_HandlesAllStatusCodes()
        {
            // Arrange
            const int evaluationId = 1;
            const int spirometryExamId = 2;

            var request = new ExamStatusEvent
            {
                EventId = Guid.NewGuid(),
                Exam = new SpirometryExam
                {
                    EvaluationId = evaluationId,
                    SpirometryExamId = spirometryExamId
                }
            };

            foreach (var statusCode in Enum.GetValues<StatusCode>())
            {
                request.StatusCode = statusCode;

                // Act

                // Would throw an exception if a case is not handled when determining if should publish to Kafka
                await CreateSubject().Handle(request, default);
            }

            Assert.True(true);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handle_WithResultsReceivedStatus_ReceivedDate_Tests(bool isOverreadResult)
        {
            // Arrange
            const int spirometryExamId = 1;
            const long appointmentId = 2;

            var overreadReceivedDate = DateTime.UtcNow;
            var pocReceivedDate = overreadReceivedDate.AddDays(-1);

            var request = new ExamStatusEvent
            {
                Exam = new SpirometryExam
                {
                    SpirometryExamId = spirometryExamId,
                    AppointmentId = appointmentId
                },
                StatusCode = StatusCode.ResultsReceived
            };

            A.CallTo(() => _mapper.Map<ResultsReceived>(A<SpirometryExam>._))
                .Returns(new ResultsReceived
                {
                    ReceivedDate = pocReceivedDate
                });

            A.CallTo(() => _mediator.Send(A<QueryExamStatus>._, A<CancellationToken>._))
                .Returns(isOverreadResult ? new ExamStatus() : null);

            A.CallTo(() => _mediator.Send(A<QueryOverreadResult>._, A<CancellationToken>._))
                .Returns(new OverreadResult
                {
                    ReceivedDateTime = overreadReceivedDate
                });

            // Act
            await CreateSubject().Handle(request, default);

            // Assert
            A.CallTo(() => _mediator.Send(A<QueryExamStatus>.That.Matches(q =>
                        q.SpirometryExamId == spirometryExamId &&
                        q.StatusCode == Signify.Spirometry.Core.Data.Entities.StatusCode.OverreadProcessed),
                    A<CancellationToken>._))
                .MustHaveHappened();

            if (!isOverreadResult)
            {
                A.CallTo(() => _mediator.Send(A<QueryOverreadResult>._, A<CancellationToken>._))
                    .MustNotHaveHappened();
            }
            else
            {
                A.CallTo(() => _mediator.Send(A<QueryOverreadResult>.That.Matches(q =>
                            q.AppointmentId == appointmentId),
                        A<CancellationToken>._))
                    .MustHaveHappened();
            }

            A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                        p.Status.ReceivedDate == (isOverreadResult ? overreadReceivedDate : pocReceivedDate)),
                    A<CancellationToken>._))
                .MustHaveHappened();
        }
    }
}