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
using Xunit;

using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.Tests.EventHandlers.NSB;

public class OrderHeldHandlerTests
{
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();

    private OrderHeldHandler CreateSubject() =>
        new OrderHeldHandler(A.Dummy<ILogger<OrderHeldHandler>>(), _transactionSupplier, _mediator, _mapper);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Handle_When_Barcode_found_in_fobt_Tests(int fobtCount)
    {
        // Arrange
        const string barcode = "barcode";

        const int fobtId = 1;
        const int duplicatefobtId = 2;

        var request = new OrderHeld
        {
            Barcode = barcode
        };

        var fobt = new Fobt
        {
            FOBTId = fobtId,
            Barcode = barcode
        };

        var duplicateFobt = new Fobt
        {
            FOBTId = duplicatefobtId,
            Barcode = barcode
        };

        var fobtListWithUniqueRecord = new List<Fobt>
        {
            fobt
        };

        var fobtListWithDuplicateRecord = new List<Fobt>
        {
            fobt,
            duplicateFobt
        };

        var fobtHistoryList = new List<FOBTBarcodeHistory> { new() { Barcode = "barcode" } };

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);

        var context = new TestableMessageHandlerContext();

        A.CallTo(() => _mediator.Send(A<GetBarcodeHistory>._, A<CancellationToken>._)).Returns(fobtHistoryList);

        switch (fobtCount)
        {
            case 0:
                A.CallTo(() => _mediator.Send(A<GetFobtByBarcode>._, A<CancellationToken>._)).Returns([]);
                break;
            case 1:
                A.CallTo(() => _mediator.Send(A<GetFobtByBarcode>._, A<CancellationToken>._)).Returns(fobtListWithUniqueRecord);
                break;
            case 2:
                A.CallTo(() => _mediator.Send(A<GetFobtByBarcode>._, A<CancellationToken>._)).Returns(fobtListWithDuplicateRecord);
                break;
        }

        // Act & Assert
        var subject = CreateSubject();
        switch (fobtCount)
        {
            case 0:
                await subject.Handle(request, context);
                A.CallTo(() => _mediator.Send(A<GetBarcodeHistory>._, A<CancellationToken>._)).MustHaveHappened();
                break;
            case 1:
                await subject.Handle(request, context);

                A.CallTo(() => _transactionSupplier.BeginTransaction()).MustHaveHappened();
                A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>.That.Matches(g =>
                            g.StatusCode == FOBTStatusCode.OrderHeld),
                        A<CancellationToken>._))
                    .MustHaveHappened();
                A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
                    .MustHaveHappened();
                A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustHaveHappened();
                break;
            case 2:
                await Assert.ThrowsAsync<DuplicateBarcodeFoundException>(async () => await subject.Handle(request, context));
                break;
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Handle_When_Barcode_found_in_fobtHistory_Tests(int historyCount)
    {
        // Arrange
        const string barcode = "barcode";

        const int fobtId = 1;
        const int duplicateFobtId = 2;

        var request = new OrderHeld
        {
            Barcode = barcode
        };

        var fobtHistory = new FOBTBarcodeHistory
        {
            FOBTId = fobtId,
            Barcode = barcode
        };

        var duplicateFobtHistory = new FOBTBarcodeHistory
        {
            FOBTId = duplicateFobtId,
            Barcode = barcode
        };

        var fobtHistoryWithUniqueRecord = new List<FOBTBarcodeHistory>
        {
            fobtHistory
        };

        var fobtHistoryWithDuplicateRecord = new List<FOBTBarcodeHistory>
        {
            fobtHistory,
            duplicateFobtHistory
        };

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);

        var context = new TestableMessageHandlerContext();

        A.CallTo(() => _mediator.Send(A<GetFobtByBarcode>._, A<CancellationToken>._)).Returns([]);

        switch (historyCount)
        {
            case 0:
                A.CallTo(() => _mediator.Send(A<GetBarcodeHistory>._, A<CancellationToken>._)).Returns([]);
                break;
            case 1:
                A.CallTo(() => _mediator.Send(A<GetBarcodeHistory>._, A<CancellationToken>._)).Returns(fobtHistoryWithUniqueRecord);
                break;
            case 2:
                A.CallTo(() => _mediator.Send(A<GetBarcodeHistory>._, A<CancellationToken>._)).Returns(fobtHistoryWithDuplicateRecord);
                break;
        }

        // Act & Assert
        var subject = CreateSubject();
        switch (historyCount)
        {
            case 0:
                await Assert.ThrowsAsync<UnableToFindFobtException>(async () => await subject.Handle(request, context));
                break;
            case 1:
                await subject.Handle(request, context);

                A.CallTo(() => _transactionSupplier.BeginTransaction()).MustHaveHappened();
                A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>.That.Matches(g =>
                            g.StatusCode == FOBTStatusCode.OrderHeld),
                        A<CancellationToken>._))
                    .MustHaveHappened();
                A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
                    .MustHaveHappened();
                A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._)).MustHaveHappened();
                break;
            case 2:
                await Assert.ThrowsAsync<DuplicateBarcodeFoundException>(async () => await subject.Handle(request, context));
                break;
        }
    }
}