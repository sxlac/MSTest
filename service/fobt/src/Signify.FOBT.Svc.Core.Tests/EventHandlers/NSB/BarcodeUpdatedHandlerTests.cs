using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers.NSB;

public class BarcodeUpdatedHandlerTests
{
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();

    private OrderUpdatedHandler CreateSubject() =>
        new OrderUpdatedHandler(A.Dummy<ILogger<OrderUpdatedHandler>>(), _transactionSupplier, _mediator, _mapper);

    [Fact]
    public async Task Handle_WhenNoCorrespondingFobtFound_Throws()
    {
        var request = new BarcodeUpdate
        {
            EvaluationId = 1
        };

        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .Returns((Fobt)null);

        var subject = CreateSubject();

        await Assert.ThrowsAsync<UnableToFindFobtException>(async () => await subject.Handle(request, default!));

        A.CallTo(() => _mediator.Send(A<GetFOBT>.That.Matches(g =>
                    g.EvaluationId == request.EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenBarcodeNotChanged_DoesNothing()
    {
        const string barcode = "barcode";
        var request = new BarcodeUpdate
        {
            EvaluationId = 1,
            Barcode = barcode
        };

        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .Returns(new Fobt { Barcode = barcode });

        var subject = CreateSubject();

        await subject.Handle(request, default!);

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustNotHaveHappened();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_HappyPath_Tests(bool isNoOrderHoldResolution)
    {
        // Arrange
        var oldBarcode = isNoOrderHoldResolution ? string.Empty : "old barcode";
        var oldOrderCorrelationId = isNoOrderHoldResolution ? Guid.Empty : Guid.NewGuid();

        const string newBarcode = "new barcode";
        const int evaluationId = 1;
        const int fobtId = 2;
        var orderCorrelationId = Guid.NewGuid();

        var request = new BarcodeUpdate
        {
            EvaluationId = evaluationId,
            Barcode = newBarcode,
            OrderCorrelationId = orderCorrelationId
        };

        var fobt = new Fobt
        {
            EvaluationId = evaluationId,
            FOBTId = fobtId,
            Barcode = oldBarcode,
            OrderCorrelationId = oldOrderCorrelationId
        };

        A.CallTo(() => _mediator.Send(A<GetFOBT>._, A<CancellationToken>._))
            .Returns(fobt);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdateFOBT>._, A<CancellationToken>._))
            .Returns(fobt);

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);

        var context = new TestableMessageHandlerContext();

        // Act
        var subject = CreateSubject();

        await subject.Handle(request, context);

        // Assert
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();

        A.CallTo(() => _mapper.Map<CreateOrUpdateFOBT>(A<Fobt>.That.Matches(f =>
                f.Barcode == newBarcode &&
                f.OrderCorrelationId == orderCorrelationId)))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<CreateBarcodeHistory>.That.Matches(h =>
                    h.FOBTId == fobtId &&
                    h.Barcode == oldBarcode &&
                    h.OrderCorrelationId == oldOrderCorrelationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        VerifyStatusesSent(fobt, new[] { FOBTStatusCode.OrderUpdated });

        if (isNoOrderHoldResolution)
        {
            VerifyStatusesSent(fobt, new[]
            {
                FOBTStatusCode.FOBTPerformed,
                FOBTStatusCode.LabOrderCreated
            });
        }
        else
        {
            VerifyStatusesNotSent(new[]
            {
                FOBTStatusCode.FOBTPerformed,
                FOBTStatusCode.LabOrderCreated
            });
        }

        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
    }

    private void VerifyStatusesSent(Fobt fobt, IEnumerable<FOBTStatusCode> statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>.That.Matches(s =>
                        s.FOBT == fobt &&
                        s.StatusCode == statusCode),
                    A<CancellationToken>._))
                .MustHaveHappened();
        }
    }

    private void VerifyStatusesNotSent(IEnumerable<FOBTStatusCode> statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>.That.Matches(s =>
                        s.StatusCode == statusCode),
                    A<CancellationToken>._))
                .MustNotHaveHappened();
        }
    }
}