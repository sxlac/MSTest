using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;
using static Signify.PAD.Svc.Core.EventHandlers.DateOfServiceUpdatedHandler;

namespace Signify.PAD.Svc.Core.Tests.EventHandlers;

public class DateOfServiceUpdatedHandlerTests : IClassFixture<MockDbFixture>
{
    private readonly DateOfServiceUpdateHandler _handler;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly MockDbFixture _mockDbFixture;

    public DateOfServiceUpdatedHandlerTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<DateOfServiceUpdateHandler>>();
        var mediator = A.Fake<IMediator>();
        var mapper = A.Fake<IMapper>();
        _transactionSupplier = A.Fake<ITransactionSupplier>();
        _mockDbFixture = mockDbFixture;
        var publishObservability = A.Fake<IPublishObservability>();
        _handler =
            new DateOfServiceUpdateHandler(logger, mediator, mockDbFixture.Context, mapper, _transactionSupplier, publishObservability);
    }

    [Fact]
    public async Task DateOfServiceUpdatedHandler_TransactionIsUsed()
    {
        var pad = new Core.Data.Entities.PAD
        {
            PADId = 1990,
            DateOfService = DateTime.UtcNow.AddDays(-5),
            EvaluationId = 1990,
        };

        var newDos = DateTime.UtcNow;
        var dateOfServiceUpdatedMessage = new DateOfServiceUpdated(1990, newDos);

        _mockDbFixture.Context.PAD.Add(pad);
        await _mockDbFixture.Context.SaveChangesAsync();

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction()).Returns(transaction);

        var context = new TestableInvokeHandlerContext();
        await _handler.Handle(dateOfServiceUpdatedMessage, context);

        A.CallTo(() => _transactionSupplier.BeginTransaction()).MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => transaction.Dispose()).MustHaveHappened();
    }
}