using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.FOBT.Svc.Core.BusinessRules;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.EventHandlers;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Messages.Queries;
using Signify.FOBT.Svc.Core.Models;
using Signify.FOBT.Svc.Core.Queries;
using System.Threading.Tasks;
using System;
using Xunit;
using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers;

public class FobtPdfDeliveredHandlerTests
{
    private readonly FobtPdfDeliveredHandler _handler;
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IBillableRules _billableRules = A.Fake<IBillableRules>();

    public FobtPdfDeliveredHandlerTests()
    {
        var logger = A.Dummy<ILogger<FobtPdfDeliveredHandler>>();
        var mapper = A.Dummy<IMapper>();

        _handler = new FobtPdfDeliveredHandler(logger, mapper, _mediator, _billableRules);
    }

    [Fact]
    public async Task Handle_NotAbleToRetrieveFobtRecord_ReturnApplicationException()
    {
        // Arrange
        var request = BuildPdfDeliveredToClient();
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns((Fobt)null);

        // Act
        // Assert
        var context = new TestableInvokeHandlerContext();
        await Assert.ThrowsAsync<ExamNotFoundException>(async () => await _handler.Handle(request, context));
    }

    [Fact]
    public async Task Handle_NotAbleToRetrieveLabWithEvaluationId_ReturnWithoutSendingBillingRequest()
    {
        // Arrange
        const int fobtEvaluationId = 12345;
        const int labPerformedEvaluationId = 22345;
        var request = BuildPdfDeliveredToClient(fobtEvaluationId);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns(Mocks.Models.FobtEntityMock.BuildFobt(fobtEvaluationId));
        A.CallTo(() => _mediator.Send(A<GetFobtStatusByStatusCodeAndEvaluationId>._, default)).Returns(Mocks.Models.FobtStatusEntityMock.BuildFobtStatus(labPerformedEvaluationId, FOBTStatusCode.FOBTPerformed));
        var context = new TestableInvokeHandlerContext();

        // Act
        await _handler.Handle(request, context);

        // Assert
        context.PublishedMessages.Length.Should().Be(0);
    }

    [Fact]
    public async Task Handle_PdfEntryIsNull_CallInsertPdfAndContinueProcess()
    {
        // Arrange
        const int evaluationId = 12345;
        var request = BuildPdfDeliveredToClient(evaluationId);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns(Mocks.Models.FobtEntityMock.BuildFobt(evaluationId));
        A.CallTo(() => _mediator.Send(A<GetFobtStatusByStatusCodeAndEvaluationId>._, default)).Returns(Mocks.Models.FobtStatusEntityMock.BuildFobtStatus(evaluationId, FOBTStatusCode.FOBTPerformed));
        A.CallTo(() => _mediator.Send(A<GetPDFToClient>._, default)).Returns((PDFToClient)null);
        A.CallTo(() => _billableRules.IsBillableForResults(A<BillableRuleAnswers>._)).Returns(new BusinessRuleStatus{IsMet = true});
        var context = new TestableInvokeHandlerContext();

        // Act
        await _handler.Handle(request, context);

        // Assert
        context.PublishedMessages.Length.Should().Be(2);
    }

    [Fact]
    public async Task Handle_FailedToInsertClientTransaction_ReturnWithoutSendingBillingRequest()
    {
        // Arrange
        const int evaluationId = 12345;
        var request = BuildPdfDeliveredToClient(evaluationId);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns(Mocks.Models.FobtEntityMock.BuildFobt(evaluationId));
        A.CallTo(() => _mediator.Send(A<GetFobtStatusByStatusCodeAndEvaluationId>._, default)).Returns(Mocks.Models.FobtStatusEntityMock.BuildFobtStatus(evaluationId, FOBTStatusCode.FOBTPerformed));
        A.CallTo(() => _mediator.Send(A<GetPDFToClient>._, default)).Returns((PDFToClient)null);
        A.CallTo(() => _mediator.Send(A<InsertPdfToClient>._, default)).Throws(new Exception());
        var context = new TestableInvokeHandlerContext();

        // Act
        // Assert
        await Assert.ThrowsAsync<Exception>(async () => await _handler.Handle(request, context));
    }

    [Fact]
    public async Task Handle_FailedToGetCorrectLabResultsReceivedAfterBilling_ReturnBeforeRunningFobtBillingLogic()
    {
        // Arrange
        const int evaluationId = 12345;
        var request = BuildPdfDeliveredToClient(evaluationId);
        var firstLabResponse = Mocks.Models.FobtStatusEntityMock.BuildFobtStatus(evaluationId, FOBTStatusCode.FOBTPerformed);
        var secondLabResponse = Mocks.Models.FobtStatusEntityMock.BuildFobtStatus(evaluationId + 1, FOBTStatusCode.ValidLabResultsReceived);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns(Mocks.Models.FobtEntityMock.BuildFobt(evaluationId));
        A.CallTo(() => _mediator.Send(A<GetFobtStatusByStatusCodeAndEvaluationId>._, default))
            .ReturnsNextFromSequence(firstLabResponse, secondLabResponse);
        A.CallTo(() => _mediator.Send(A<GetPDFToClient>._, default)).Returns(Mocks.Models.PdfToClientMock.BuildPdfToClient());
        var context = new TestableInvokeHandlerContext();

        // Act
        await _handler.Handle(request, context);

        // Assert
        context.PublishedMessages.Length.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SkipSendingBillingRequestBecauseFobtIsNotBillable_ReturnWithoutSendingBillingRequest()
    {
        // Arrange
        const int evaluationId = 12345;
        var request = BuildPdfDeliveredToClient(evaluationId);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns(Mocks.Models.FobtEntityMock.BuildFobt(evaluationId, "NY"));
        A.CallTo(() => _mediator.Send(A<GetFobtStatusByStatusCodeAndEvaluationId>._, default)).Returns(Mocks.Models.FobtStatusEntityMock.BuildFobtStatus(evaluationId, FOBTStatusCode.FOBTPerformed));
        A.CallTo(() => _mediator.Send(A<GetPDFToClient>._, default)).Returns(Mocks.Models.PdfToClientMock.BuildPdfToClient());
        A.CallTo(() => _billableRules.IsBillableForResults(A<BillableRuleAnswers>._)).Returns(new BusinessRuleStatus{IsMet = false});

        var context = new TestableInvokeHandlerContext();

        // Act
        await _handler.Handle(request, context);

        // Assert
        context.PublishedMessages.Length.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SkipSendingBillingRequestBecauseFobtIsNotBillableAndNotAbleToGetLabResults_ReturnWithoutSendingBillingRequest()
    {
        // Arrange
        const int evaluationId = 12345;
        var request = BuildPdfDeliveredToClient(evaluationId);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns(Mocks.Models.FobtEntityMock.BuildFobt(evaluationId, "NY"));
        A.CallTo(() => _mediator.Send(A<GetFobtStatusByStatusCodeAndEvaluationId>._, default)).Returns(Mocks.Models.FobtStatusEntityMock.BuildFobtStatus(evaluationId, FOBTStatusCode.FOBTPerformed));
        A.CallTo(() => _mediator.Send(A<GetPDFToClient>._, default)).Returns(Mocks.Models.PdfToClientMock.BuildPdfToClient());
        A.CallTo(() => _mediator.Send(A<GetLabResultByFobtId>._, default)).Returns((LabResults)null);
        A.CallTo(() => _billableRules.IsBillableForResults(A<BillableRuleAnswers>._)).Returns(new BusinessRuleStatus{IsMet = false});

        var context = new TestableInvokeHandlerContext();

        // Act
        await _handler.Handle(request, context);

        // Assert
        context.PublishedMessages.Length.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SendBillingRequestBecauseFobtIsBillable_SendingBillingRequest()
    {
        // Arrange
        const int evaluationId = 12345;
        var request = BuildPdfDeliveredToClient(evaluationId);
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns(Mocks.Models.FobtEntityMock.BuildFobt(evaluationId, "TX"));
        A.CallTo(() => _mediator.Send(A<GetFobtStatusByStatusCodeAndEvaluationId>._, default)).Returns(Mocks.Models.FobtStatusEntityMock.BuildFobtStatus(evaluationId, FOBTStatusCode.FOBTPerformed));
        A.CallTo(() => _mediator.Send(A<GetPDFToClient>._, default)).Returns(Mocks.Models.PdfToClientMock.BuildPdfToClient());
        A.CallTo(() => _billableRules.IsBillableForResults(A<BillableRuleAnswers>._)).Returns(new BusinessRuleStatus{IsMet = true});
        var context = new TestableInvokeHandlerContext();

        // Act
        await _handler.Handle(request, context);

        // Assert
        context.PublishedMessages.Length.Should().Be(2);
    }

    [Fact]
    public async Task Handle_SendBillingRequestWithNullClientId_ThrowException()
    {
        // Arrange
        const int evaluationId = 12345;
        var request = BuildPdfDeliveredToClient(evaluationId);
        var fobt = Mocks.Models.FobtEntityMock.BuildFobt(evaluationId, "TX");
        fobt.ClientId = null;
        A.CallTo(() => _mediator.Send(A<GetFOBT>._, default)).Returns(fobt);
        A.CallTo(() => _mediator.Send(A<GetFobtStatusByStatusCodeAndEvaluationId>._, default)).Returns(Mocks.Models.FobtStatusEntityMock.BuildFobtStatus(evaluationId, FOBTStatusCode.FOBTPerformed));
        A.CallTo(() => _mediator.Send(A<GetPDFToClient>._, default)).Returns(Mocks.Models.PdfToClientMock.BuildPdfToClient());
        var context = new TestableInvokeHandlerContext();

        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await _handler.Handle(request, context));
    }

    private static PdfDeliveredToClient BuildPdfDeliveredToClient(int? evaluationId = 123456)
    {
        return new PdfDeliveredToClient
        { 
            EvaluationId = evaluationId!.Value
        };
    }
}