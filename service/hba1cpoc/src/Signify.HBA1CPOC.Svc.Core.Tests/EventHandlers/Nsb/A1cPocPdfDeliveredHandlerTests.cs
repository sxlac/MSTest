using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using Signify.HBA1CPOC.Svc.Core.Exceptions;
using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.StaticEntity;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using HbA1cPoc = Signify.HBA1CPOC.Svc.Core.Data.Entities.HBA1CPOC;

namespace Signify.HBA1CPOC.Svc.Core.Tests.EventHandlers.Nsb;

public class A1cPocPdfDeliveredHandlerTests : IClassFixture<MockDbFixture>
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly A1cPocPdfDeliveredHandler _handler;
    private readonly FakeTransactionSupplier _transactionSupplier = new();

    public A1cPocPdfDeliveredHandlerTests()
    {
        var logger = A.Dummy<ILogger<A1cPocPdfDeliveredHandler>>();

        _handler = new A1cPocPdfDeliveredHandler(logger, A.Dummy<IMapper>(), _mediator, _transactionSupplier);
    }

    [Fact]
    public async Task A1cPocPdfDeliveredHandle_NotAbleToRetrieveA1cPocRecordWithEvaluation_ThrowExamDoesNotExistException()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, default)).Returns((HbA1cPoc)null);
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, default)).Returns(new EvaluationAnswers { IsHBA1CEvaluation = true });

        // Act
        // Assert
        var context = new TestableInvokeHandlerContext();
        await Assert.ThrowsAsync<ExamNotFoundException>(async () => await _handler.Handle(request, context));

        _transactionSupplier.AssertRollback();
    }

    [Fact]
    public async Task A1cPocPdfDeliveredHandle_NotAbleToRetrieveA1cPocRecordButWithoutEvaluation_ThrowsExceptionWithoutRunningLogic()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, default)).Returns((HbA1cPoc)null);
        A.CallTo(() => _mediator.Send(A<CheckHBA1CPOCEval>._, default)).Returns(new EvaluationAnswers { IsHBA1CEvaluation = false });

        // Act
        var context = new TestableInvokeHandlerContext();
        await Assert.ThrowsAsync<ExamNotFoundException>(async () => await _handler.Handle(request, context));

        // Assert
        A.CallTo(() => _mediator.Send(A<GetHba1CPocStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<InsertPdfToClientTransaction>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
        context.SentMessages.Length.Should().Be(0);

        _transactionSupplier.AssertRollback();
    }

    [Fact]
    public async Task A1cPocPdfDeliveredHandle_NotAbleToRetrievePerformedStatus_DoNotPublishBillingRequest()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, default)).Returns(StaticMockEntities.Hba1Cpoc);
        A.CallTo(() => _mediator.Send(A<GetHba1CPocStatus>._, default)).Returns((HBA1CPOCStatus)null);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns((PDFToClient)null);

        // Act
        var context = new TestableInvokeHandlerContext();
        await _handler.Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<InsertPdfToClientTransaction>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
        context.SentMessages.Length.Should().Be(0);

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task A1cPocPdfDeliveredHandle_DoesNotGetPdfEntry_PublishBillingRequest()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, default)).Returns(StaticMockEntities.Hba1Cpoc);
        var hbA1cPocStatus = StaticMockEntities.CreateHbA1cPocStatus(HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCode.HBA1CPOCPerformed.StatusCodeName);
        request.EvaluationId = (int)StaticMockEntities.Hba1Cpoc.EvaluationId;
        A.CallTo(() => _mediator.Send(A<GetHba1CPocStatus>._, default)).Returns(hbA1cPocStatus);
        PDFToClient pdfEntry = null;
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(pdfEntry);

        // Act
        var context = new TestableInvokeHandlerContext();
        await _handler.Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<InsertPdfToClientTransaction>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        context.SentMessages.Length.Should().Be(1);

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task A1cPocPdfDeliveredHandle_GetsPdfEntry_PublishBillingRequest()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        A.CallTo(() => _mediator.Send(A<GetHBA1CPOC>._, default)).Returns(StaticMockEntities.Hba1Cpoc);
        var hbA1cPocStatus = StaticMockEntities.CreateHbA1cPocStatus(HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCode.HBA1CPOCPerformed.StatusCodeName);
        request.EvaluationId = (int)StaticMockEntities.Hba1Cpoc.EvaluationId;
        A.CallTo(() => _mediator.Send(A<GetHba1CPocStatus>._, default)).Returns(hbA1cPocStatus);
        A.CallTo(() => _mediator.Send(A<GetPdfToClient>._, A<CancellationToken>._)).Returns(StaticMockEntities.CreatePdfToClient);

        // Act
        var context = new TestableInvokeHandlerContext();
        await _handler.Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<InsertPdfToClientTransaction>._, A<CancellationToken>._)).MustNotHaveHappened();
        context.SentMessages.Length.Should().Be(1);

        _transactionSupplier.AssertCommit();
    }

    private static PdfDeliveredToClient BuildPdfDeliveredToClient(int evaluationId = 123456)
    {
        return new PdfDeliveredToClient
        {
            EvaluationId = evaluationId
        };
    }
}