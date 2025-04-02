using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus.Testing;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.EventHandlers;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Exceptions;
using Signify.CKD.Svc.Core.FeatureFlagging;
using Signify.CKD.Svc.Core.Messages.Status;
using Signify.CKD.Svc.Core.Models;
using Signify.CKD.Svc.Core.Queries;
using Signify.CKD.Svc.Core.Tests.Mocks.StaticEntity;
using Signify.CKD.Svc.Core.Tests.Utilities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers;

public class CKDPdfDeliveredHandlerTests : IClassFixture<EntityFixtures>, IClassFixture<MockDbFixture>
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly CKDPdfDeliveredHandler _handler;
    private readonly IFeatureFlags _featureFlags = A.Fake<IFeatureFlags>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();

    public CKDPdfDeliveredHandlerTests()
    {
        var logger = A.Dummy<ILogger<CKDPdfDeliveredHandler>>();
        _handler = new CKDPdfDeliveredHandler(logger, _mapper, _mediator, _featureFlags, A.Dummy<IAgent>(), _transactionSupplier);
    }

    [Fact]
    public async Task CKDPdfDeliveredHandle_NotAbleToRetrieveCKDRecordWithEvaluation_ThrowExamNotFoundException()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        Core.Data.Entities.CKD ckd = null;
        A.CallTo(() => _mediator.Send(A<GetCKD>._, default)).Returns(ckd);
        A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, default)).Returns(new EvaluationAnswers { IsCKDEvaluation = true });

        // Act
        // Assert
        var context = new TestableInvokeHandlerContext();
        await Assert.ThrowsAsync<ExamNotFoundException>(async () => await _handler.Handle(request, context));

        _transactionSupplier.AssertNoTransactionCreated();
    }

    [Fact]
    public async Task CKDPdfDeliveredHandle_NotAbleToRetrieveCKDRecordButWithoutEvaluation_ReturnsWithoutRunningLogic()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        Core.Data.Entities.CKD ckd = null;
        A.CallTo(() => _mediator.Send(A<GetCKD>._, default)).Returns(ckd);
        A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, default)).Returns(new EvaluationAnswers { IsCKDEvaluation = false });

        // Act
        var context = new TestableInvokeHandlerContext();

        // Assert
        await Assert.ThrowsAsync<ExamNotFoundException>(async () => await _handler.Handle(request, context));
        A.CallTo(() => _mediator.Send(A<GetCKDStatuses>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<InsertPdfToClientTransaction>._, A<CancellationToken>._)).MustNotHaveHappened();
        context.SentMessages.Length.Should().Be(0);
        context.PublishedMessages.Length.Should().Be(0);

        _transactionSupplier.AssertNoTransactionCreated();
    }

    [Fact]
    public async Task CKDPdfDeliveredHandle_NotAbleToRetrievePerformedStatus_DoNotPublishBillingRequest()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        A.CallTo(() => _mediator.Send(A<GetCKD>._, default)).Returns(StaticMockEntities.CKD);
        var ckdStatuses = new List<CKDStatus>();
        A.CallTo(() => _mediator.Send(A<GetCKDStatuses>._, default)).Returns(ckdStatuses);

        // Act
        var context = new TestableInvokeHandlerContext();

        // Assert
        await Assert.ThrowsAsync<UnableToDetermineBillabilityException>(async () => await _handler.Handle(request, context));
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<InsertPdfToClientTransaction>._, A<CancellationToken>._)).MustNotHaveHappened();
        context.SentMessages.Length.Should().Be(0);
        context.PublishedMessages.Length.Should().Be(0);

        _transactionSupplier.AssertNoTransactionCreated();
    }

    [Fact]
    public async Task CKDPdfDeliveredHandle_GetsPdfEntry_Performed_Valid_PublishBillingRequest()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        var ckdStatus = StaticMockEntities.CreateCKDStatus(CKDStatusCode.CKDPerformed.CKDStatusCodeId, CKDStatusCode.CKDPerformed.StatusCode);
        var ckdStatuses = new List<CKDStatus> { ckdStatus };
        request.EvaluationId = ckdStatus.CKD.EvaluationId!.Value;
        var ckd = StaticMockEntities.CKD;
        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._)).Returns(ckd);
        A.CallTo(() => _mediator.Send(A<GetCKDStatuses>._, default)).Returns(ckdStatuses);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(StaticMockEntities.CreatePdfToClient);

        // Act
        var context = new TestableInvokeHandlerContext();
        await _handler.Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<InsertPdfToClientTransaction>._, A<CancellationToken>._)).MustHaveHappened();
        context.SentMessages.Length.Should().Be(1);
        context.PublishedMessages.Length.Should().Be(0);
        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task CKDPdfDeliveredHandle_GetsPdfEntry_Performed_InValid_NoMessagePublished()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        var ckdStatus = StaticMockEntities.CreateCKDStatus(CKDStatusCode.CKDPerformed.CKDStatusCodeId, CKDStatusCode.CKDPerformed.StatusCode);
        List<CKDStatus> ckdStatuses = new List<CKDStatus> { ckdStatus };
        request.EvaluationId = ckdStatus.CKD.EvaluationId!.Value;

        var ckdEntity = StaticMockEntities.CKD;

        //Setting CKD Answer to invalid
        ckdEntity.CKDAnswer = null;

        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._)).Returns(ckdEntity);
        A.CallTo(() => _mediator.Send(A<GetCKDStatuses>._, default)).Returns(ckdStatuses);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(StaticMockEntities.CreatePdfToClient);

        // Act
        var context = new TestableInvokeHandlerContext();
        await _handler.Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<InsertPdfToClientTransaction>._, A<CancellationToken>._)).MustHaveHappened();
        context.SentMessages.Length.Should().Be(0);
        context.PublishedMessages.Length.Should().Be(0);

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task CKDPdfDeliveredHandle_GetsPdfEntry_NotPerformed_Valid_NoMessagePublished()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        var ckdStatus = StaticMockEntities.CreateCKDStatus(CKDStatusCode.CKDNotPerformed.CKDStatusCodeId, CKDStatusCode.CKDNotPerformed.StatusCode);
        var ckdStatuses = new List<CKDStatus>() { ckdStatus };
        request.EvaluationId = ckdStatus.CKD.EvaluationId!.Value;

        var ckdEntity = StaticMockEntities.CKD;

        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._)).Returns(ckdEntity);
        A.CallTo(() => _mediator.Send(A<GetCKDStatuses>._, default)).Returns(ckdStatuses);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(StaticMockEntities.CreatePdfToClient);

        // Act
        var context = new TestableInvokeHandlerContext();
        await _handler.Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<InsertPdfToClientTransaction>._, A<CancellationToken>._)).MustHaveHappened();
        context.SentMessages.Length.Should().Be(0);
        context.PublishedMessages.Length.Should().Be(0);

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task CKDPdfDeliveredHandle_GetsPdfEntry_NotPerformed_InValid_NoMessagePublished()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        var ckdStatus = StaticMockEntities.CreateCKDStatus(CKDStatusCode.CKDNotPerformed.CKDStatusCodeId, CKDStatusCode.CKDNotPerformed.StatusCode);
        List<CKDStatus> ckdStatuses = new List<CKDStatus>() { ckdStatus };
        request.EvaluationId = ckdStatus.CKD.EvaluationId!.Value;

        var ckdEntity = StaticMockEntities.CKD;

        //Setting CKD Answer to invalid
        ckdEntity.CKDAnswer = null;

        A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._)).Returns(ckdEntity);
        A.CallTo(() => _mediator.Send(A<GetCKDStatuses>._, default)).Returns(ckdStatuses);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(StaticMockEntities.CreatePdfToClient);

        // Act
        var context = new TestableInvokeHandlerContext();
        await _handler.Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<InsertPdfToClientTransaction>._, A<CancellationToken>._)).MustHaveHappened();
        context.SentMessages.Length.Should().Be(0);
        context.PublishedMessages.Length.Should().Be(0);

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task CKDPdfDeliveredHandle_CKDNotPerformed_BRNSStatusRecordedAndKafkaPublished()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        var ckdStatus = StaticMockEntities.CreateCKDStatus(CKDStatusCode.CKDNotPerformed.CKDStatusCodeId, CKDStatusCode.CKDNotPerformed.StatusCode);
        List<CKDStatus> ckdStatuses = new List<CKDStatus>() { ckdStatus };
        ckdStatuses[0].CKD = StaticMockEntities.CKD;
        ckdStatuses[0].CKD.CKDId = +10;
        request.EvaluationId = ckdStatus.CKD.EvaluationId!.Value;
        A.CallTo(() => _mediator.Send(A<GetCKDStatuses>._, default)).Returns(ckdStatuses);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(StaticMockEntities.CreatePdfToClient);

        // Act
        var context = new TestableInvokeHandlerContext();
        await _handler.Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
            p.Status is BillRequestNotSent), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateCKDStatus>.That.Matches(c =>
            c.StatusCodeId == CKDStatusCode.BillRequestNotSent.CKDStatusCodeId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        context.SentMessages.Length.Should().Be(0);
        context.PublishedMessages.Length.Should().Be(0);
        _transactionSupplier.AssertCommit();
    }


    private static PdfDeliveredToClient BuildPdfDeliveredToClient(int? evaluationId = 123456)
    {
        return new PdfDeliveredToClient
        {
            EvaluationId = evaluationId!.Value
        };
    }
}