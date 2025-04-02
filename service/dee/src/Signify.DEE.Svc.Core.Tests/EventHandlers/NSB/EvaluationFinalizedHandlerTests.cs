using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.DEE.Core.Messages.Queries;
using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.EventHandlers.Nsb;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DeeNotPerformed = Signify.DEE.Svc.Core.Data.Entities.DeeNotPerformed;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers.NSB;

public class EvaluationFinalizedHandlerTests
{
    private readonly EvaluationFinalizedHandler _handler;
    private readonly TestableMessageHandlerContext _messageHandlerContext = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly FakeApplicationTime _applicationTime = new();

    public EvaluationFinalizedHandlerTests()
    {
        var logger = A.Dummy<ILogger<EvaluationFinalizedHandler>>();

        _handler = new EvaluationFinalizedHandler(logger,
            _mediator,
            _mapper,
            _transactionSupplier,
            _publishObservability,
            _applicationTime);
    }

    private void SetupForDosUpdate(bool isPerformed)
    {
        A.CallTo(() => _mediator.Send(A<GetEvalAnswers>._, A<CancellationToken>._))
            .Returns(new ExamAnswersModel
            {
                Images = new List<string>(isPerformed ?
                    new[] { "image1" } : Enumerable.Empty<string>())
            });

        A.CallTo(() => _mediator.Send(A<CreateExamRecord>._, A<CancellationToken>._))
            .Returns(new CreateExamRecordResponse(new ExamModel(), false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenAlreadyFinalized_CommitsTransactionForDosUpdates(bool isPerformed)
    {
        // Arrange
        SetupForDosUpdate(isPerformed);

        // Act
        await _handler.Handle(new EvaluationFinalizedEvent() { Products = new List<Product>() { new Product("DEE") } }, _messageHandlerContext);

        // Assert
        _transactionSupplier.AssertCommit();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenAlreadyFinalized_DoesNotRecordDuplicateInfo(bool isPerformed)
    {
        // Arrange
        SetupForDosUpdate(isPerformed);

        // Act
        await _handler.Handle(new EvaluationFinalizedEvent() { Products = new List<Product>() { new Product("DEE") } }, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateIrisOrder>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Should_Map_Evaluation_CreatedDateTime_To_Exam_CreatedDateTime()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<CreateExamRecord>._, A<CancellationToken>._)).Returns(new CreateExamRecordResponse(new ExamModel(), true));

        //Act
        await _handler.Handle(_message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateExamRecord>.That.Matches(p => p.CreatedDateTime == _message.CreatedDateTime.UtcDateTime), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ShouldUpdateStatus_WhenNoImagesTaken()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));

        A.CallTo(() => _mediator.Send(A<CreateExamRecord>._, A<CancellationToken>._))
            .Returns(new CreateExamRecordResponse(new ExamModel(), true));

        //Act
        await _handler.Handle(_message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).MustHaveHappenedTwiceOrMore();
        _messageHandlerContext.SentMessages.Length.Should().Be(1);
    }

    [Theory]
    [InlineData("Exam Created")]
    [InlineData("No DEE Images Taken")]
    [InlineData("DEE Not Performed")]
    public async Task ShouldUpdateStatusesWithDeeNotPerformedReason_WhenExamIsNotPerformed(string examStatusCode)
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<GetNotPerformedModel>._, A<CancellationToken>._)).Returns(new NotPerformedModel() { AnswerId = 1, NotPerformedReasonId = 1, Reason = "Patient Unwilling" });
        A.CallTo(() => _mediator.Send(A<AddDeeNotPerformed>._, A<CancellationToken>._)).Returns(new DeeNotPerformed() { DeeNotPerformedId = 1, ExamId = 1, NotPerformedReasonId = 1 });

        A.CallTo(() => _mediator.Send(A<CreateExamRecord>._, A<CancellationToken>._))
            .Returns(new CreateExamRecordResponse(new ExamModel(), true));

        //Act
        await _handler.Handle(_message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(
                s => s.ExamStatusCode.Name.Equals(examStatusCode)), A<CancellationToken>._)
        ).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<GetNotPerformedModel>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<AddDeeNotPerformed>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p => p.Status is Performed), A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p => p.Status is NotPerformed), A<CancellationToken>._))
            .MustHaveHappened();
        _messageHandlerContext.SentMessages.Length.Should().Be(1);

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappenedTwiceExactly();
    }

    [Theory]
    [InlineData("Exam Created")]
    [InlineData("No DEE Images Taken")]
    [InlineData("DEE Not Performed")]
    public async Task ShouldUpdateStatusWithNullNotPerformedReason_WhenImageCountZero(string examStatusCode)
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        NotPerformedModel notPerformedModel = null;
        A.CallTo(() => _mediator.Send(A<GetNotPerformedModel>._, A<CancellationToken>._)).Returns(notPerformedModel);
        A.CallTo(() => _mediator.Send(A<AddDeeNotPerformed>._, A<CancellationToken>._)).Returns(new DeeNotPerformed() { DeeNotPerformedId = 1, ExamId = 1, NotPerformedReasonId = 1 });

        A.CallTo(() => _mediator.Send(A<CreateExamRecord>._, A<CancellationToken>._))
            .Returns(new CreateExamRecordResponse(new ExamModel(), true));

        //Act
        await _handler.Handle(_message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(
                s => s.ExamStatusCode.Name.Equals(examStatusCode)), A<CancellationToken>._)
        ).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<AddDeeNotPerformed>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<GetNotPerformedModel>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        _messageHandlerContext.SentMessages.Length.Should().Be(1);
    }

    [Theory]
    [InlineData("Exam Created")]
    [InlineData("DEE Images Found")]
    [InlineData("DEE Performed")]
    [InlineData("IrisOrderSubmitted")]
    [InlineData("IrisImagesSubmitted")]
    public async Task ShouldUpdateStatuses_WhenExamIsPerformed(string examStatusCode)
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<GetEvalAnswers>._, A<CancellationToken>._)).Returns(_examAnswerModelSingleImage);
        A.CallTo(() => _mediator.Send(A<CreateExamRecord>._, A<CancellationToken>._)).Returns(new CreateExamRecordResponse(_examModel, true));
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<CreateExamImage>._, A<CancellationToken>._)).Returns(new ExamImage() { ImageLocalId = Guid.NewGuid().ToString() });

        A.CallTo(() => _mediator.Send(A<CreateExamRecord>._, A<CancellationToken>._))
            .Returns(new CreateExamRecordResponse(new ExamModel(), true));

        // Act
        await _handler.Handle(_message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(
                s => s.ExamStatusCode.Name.Equals(examStatusCode)), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p => p.Status is Performed), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p => p.Status is NotPerformed), A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateIrisOrder>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        _transactionSupplier.AssertCommit();

        A.CallTo(() => _publishObservability.RegisterEvent(A<ObservabilityEvent>._, true)).MustHaveHappenedANumberOfTimesMatching(t => t == 2);
    }

    [Fact]
    public async Task ShouldSetReceivedDateFromEvaluation()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<CreateExamRecord>._, A<CancellationToken>._)).Returns(new CreateExamRecordResponse(_examModel, true));

        // Act
        await _handler.Handle(_message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateExamRecord>.
                That.
                Matches(
                    s => s.ReceivedDateTime.Equals(_message.ReceivedDateTime)),
            A<CancellationToken>._)
        ).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_ShouldCorrectlyPersistRetinalNotesForPerformed()
    {
        // Arrange
        var retinalImageTestingNotes = "Some Retinal Testing Notes";
        var examModel = new ExamModel
        {
            RetinalImageTestingNotes = retinalImageTestingNotes
        };

        A.CallTo(() => _mediator.Send(A<CreateExamRecord>._, A<CancellationToken>._))
            .Returns(new CreateExamRecordResponse(examModel, true));


        A.CallTo(() => _mediator.Send(A<GetEvalAnswers>._, A<CancellationToken>._)).Returns(_examAnswerModelSingleImage);

        A.CallTo(() => _mediator.Send(A<CreateExamImage>._, A<CancellationToken>._)).Returns(new ExamImage() { ImageLocalId = Guid.NewGuid().ToString() });


        // Act
        await _handler.Handle(_message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateExamRecord>.That.Matches(
                p => p.RetinalImageTestingNotes == retinalImageTestingNotes), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_ShouldCorrectlyPersistRetinalNotesForNotPerformed()
    {
        // Arrange
        var retinalImageTestingNotes = "Some Retinal Testing Notes";
        var examModel = new ExamModel
        {
            RetinalImageTestingNotes = retinalImageTestingNotes
        };

        A.CallTo(() => _mediator.Send(A<CreateExamRecord>._, A<CancellationToken>._))
            .Returns(new CreateExamRecordResponse(examModel, true));

        var examAnswersModel = new ExamAnswersModel
        {
            RetinalImageTestingNotes = retinalImageTestingNotes
        };

        A.CallTo(() => _mediator.Send(A<GetEvalAnswers>._, A<CancellationToken>._)).Returns(examAnswersModel);

        // Act
        await _handler.Handle(_message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateExamRecord>.That.Matches(
                p => p.RetinalImageTestingNotes == retinalImageTestingNotes), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_ShouldReleaseCdiHoldWhenNotPerformedAndHoldExists()
    {
        // Arrange
        var retinalImageTestingNotes = "Some Retinal Testing Notes";
        var examModel = new ExamModel
        {
            RetinalImageTestingNotes = retinalImageTestingNotes
        };

        A.CallTo(() => _mediator.Send(A<CreateExamRecord>._, A<CancellationToken>._))
            .Returns(new CreateExamRecordResponse(examModel, true));

        var examAnswersModel = new ExamAnswersModel
        {
            RetinalImageTestingNotes = retinalImageTestingNotes
        };

        A.CallTo(() => _mediator.Send(A<GetEvalAnswers>._, A<CancellationToken>._)).Returns(examAnswersModel);

        A.CallTo(() => _mediator.Send(A<CreateExamImage>._, A<CancellationToken>._)).Returns(new ExamImage() { ImageLocalId = Guid.NewGuid().ToString() });

        A.CallTo(() => _mediator.Send(A<GetHold>._, A<CancellationToken>._)).Returns(new Hold() { HoldId = 5 });

        // Act
        await _handler.Handle(_message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateExamRecord>.That.Matches(
                p => p.RetinalImageTestingNotes == retinalImageTestingNotes), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        _messageHandlerContext.SentMessages.Length.Should().Be(1);
        var reloadHoldCall = (ReleaseHold)_messageHandlerContext.SentMessages[0].Message;
        Assert.IsType<ReleaseHold>(reloadHoldCall);
    }
    
    [Fact]
    public async Task Handle_ShouldCorrectlyPersistEnucleationForPerformed()
    {
        // Arrange
        var hasEnucleation = true;
        var examModel = new ExamModel
        {
            HasEnucleation = hasEnucleation
        };

        A.CallTo(() => _mediator.Send(A<CreateExamRecord>._, A<CancellationToken>._))
            .Returns(new CreateExamRecordResponse(examModel, true));


        A.CallTo(() => _mediator.Send(A<GetEvalAnswers>._, A<CancellationToken>._)).Returns(_examAnswerModelSingleImage);

        A.CallTo(() => _mediator.Send(A<CreateExamImage>._, A<CancellationToken>._)).Returns(new ExamImage() { ImageLocalId = Guid.NewGuid().ToString() });


        // Act
        await _handler.Handle(_message, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateExamRecord>.That.Matches(
                p => p.HasEnucleation == hasEnucleation), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    private readonly ExamAnswersModel _examAnswerModelSingleImage = new ExamAnswersModel
    {
        DateOfService = DateTime.Now,
        State = "NY",
        MemberPlanId = 123,
        MemberLastName = "Tim",
        MemberGender = "Male",
        MemberBirthDate = default,
        ProviderId = "wsdl",
        ProviderFirstName = "as",
        ProviderNpi = "azx",
        ProviderEmail = "test@test.com",
        Images = new List<string> { "Image1" },
        RetinalImageTestingNotes = "Some Retinal Testing Notes",
        HasEnucleation = true
    };

    private readonly ExamModel _examModel = new ExamModel
    {
        DateOfService = DateTime.Now,
        State = "NY",
        MemberPlanId = 123,
        ProviderId = 12,
    };

    private readonly EvaluationFinalizedEvent _message = new EvaluationFinalizedEvent
    {
        Id = Guid.NewGuid(),
        EvaluationId = 121,
        EvaluationTypeId = 2,
        FormVersionId = 3,
        ProviderId = default,
        UserName = "caspar",
        AppointmentId = 4,
        ApplicationId = "signifyHealth",
        MemberId = 7,
        ClientId = 8,
        DocumentPath = "",
        CreatedDateTime = DateTime.UtcNow,
        ReceivedDateTime = DateTime.Now,
        Products = new List<Product> { new Product("ASA"), new Product("DEE") }
    };
}