using AutoMapper;
using FakeItEasy;
using MediatR;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Events.Status;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Events;

public class ExamStatusEventHandlerTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();

    private ExamStatusEventHandler CreateSubject()
        => new(_mapper, _mediator);

    [Theory]
    // To be expanded upon in ANC-3978
    [InlineData(StatusCodes.BillRequestNotSent)]
    public async Task Handle_WithPublishableStatus_PublishesStatusEventToKafka(StatusCodes statusCode)
    {
        // Arrange
        const int evaluationId = 1;

        var request = new ExamStatusEventNew
        {
            Exam = new Core.Data.Entities.PAD
            {
                EvaluationId = evaluationId
            },
            StatusCode = statusCode
        };

        // Act
        await CreateSubject().Handle(request, default);

        // Assert
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._))
            .MustHaveHappened();

        switch (statusCode)
        {
            case StatusCodes.BillRequestNotSent:
                A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>.That.Matches(q =>
                        q.EvaluationId == evaluationId), A<CancellationToken>._))
                    .MustHaveHappened();
                A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                        p.Status is BillRequestNotSent), A<CancellationToken>._))
                    .MustHaveHappened();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "Status code passed into test has no assertions");
        }
    }

    [Theory]
    // To be expanded upon in ANC-3978
    [InlineData(StatusCodes.BillableEventReceived)]
    [InlineData(StatusCodes.WaveformDocumentDownloaded)]
    [InlineData(StatusCodes.WaveformDocumentUploaded)]
    public async Task Handle_WithoutPublishableStatus_DoesNotPublishStatusEventToKafka(StatusCodes statusCode)
    {
        // Arrange
        var request = new ExamStatusEventNew
        {
            Exam = new Core.Data.Entities.PAD
            {
                EvaluationId = 1
            },
            StatusCode = statusCode
        };

        // Act
        await CreateSubject().Handle(request, default);

        // Assert
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenDeterminingWhetherToPublishToKafka_HandlesAllStatusCodes()
    {
        // Arrange
        var request = new ExamStatusEventNew
        {
            Exam = new Core.Data.Entities.PAD
            {
                EvaluationId = 1
            }
        };

        foreach (var statusCode in Enum.GetValues<StatusCodes>())
        {
            request.StatusCode = statusCode;

            // Act

            // Would throw an exception if a case is not handled when determining if should publish to Kafka
            await CreateSubject().Handle(request, default);
        }

        Assert.True(true);
    }

    [Fact]
    public async Task Handle_WithAnyStatusCode_SavesStatusToDb()
    {
        // Arrange
        var request = new ExamStatusEventNew
        {
            Exam = new Core.Data.Entities.PAD
            {
                EvaluationId = 1
            }
        };

        A.CallTo(() => _mapper.Map<PADStatus>(A<ExamStatusEventNew>._))
            .ReturnsLazily(call =>
                new PADStatus(default, (int)call.Arguments.Get<ExamStatusEventNew>(0)!.StatusCode, default, default));

        foreach (var statusCode in Enum.GetValues<StatusCodes>())
        {
            request.StatusCode = statusCode;

            // Act
            await CreateSubject().Handle(request, default);

            // Assert
            A.CallTo(() => _mediator.Send(A<AddExamStatus>.That.Matches(a =>
                    a.Status.PADStatusCodeId == (int)statusCode), A<CancellationToken>._))
                .MustHaveHappened();
        }
    }
}