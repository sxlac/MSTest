using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public sealed class AddCdiEventForPaymentHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();
    private readonly IMediator _mediator = A.Fake<IMediator>();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private AddCdiEventForPaymentHandler CreateSubject()
        => new AddCdiEventForPaymentHandler(A.Dummy<ILogger<AddCdiEventForPaymentHandler>>(), _dbFixture.SharedDbContext, _mediator);

    [Fact]
    public async Task Handle_WhereMatchingEntityFound_DoesNotAddAnother()
    {
        // Arrange
        const int evaluationId = 1234;
        var requestId = Guid.NewGuid();
        var expectedCdiEvent = new CdiEventForPayment
        {
            EvaluationId = evaluationId,
            RequestId = requestId,
            DateTime = new FakeApplicationTime().UtcNow().AddDays(-1),
            EventType = "CDIFailedEvent",
            PayProvider = false,
            ApplicationId = "DPS_App"
        };
        await _dbFixture.SharedDbContext.CdiEventForPayments.AddAsync(expectedCdiEvent);
        await _dbFixture.SharedDbContext.SaveChangesAsync();
        A.CallTo(() => _mediator.Send(A<QueryCdiEventForPayment>._, A<CancellationToken>._)).Returns(expectedCdiEvent);
        var countCdiEvents = _dbFixture.SharedDbContext.CdiEventForPayments.Count();
        var newCdiEvent = new CdiEventForPayment
        {
            EvaluationId = evaluationId,
            RequestId = requestId
        };
        var request = new AddCdiEventForPayment(newCdiEvent);

        //Act
        var subject = CreateSubject();
        var actual = await subject.Handle(request, default);

        //Assert
        Assert.Equal(expectedCdiEvent, actual.CdiEventForPayment);
        Assert.False(actual.IsNew);
        Assert.Equal(countCdiEvents, _dbFixture.SharedDbContext.CdiEventForPayments.Count());
    }

    [Fact]
    public async Task Handle_WhereNoMatchingEntityFound_AddsToDb()
    {
        // Arrange
        const int evaluationId = 1234;
        var requestId = Guid.NewGuid();
        var expectedCdiEvent = new CdiEventForPayment
        {
            EvaluationId = evaluationId,
            RequestId = requestId,
            DateTime = new FakeApplicationTime().UtcNow().AddDays(-1),
            EventType = "CDIFailedEvent",
            PayProvider = false,
            ApplicationId = "DpsUser"
        };
        A.CallTo(() => _mediator.Send(A<QueryCdiEventForPayment>._, A<CancellationToken>._)).Returns((CdiEventForPayment)null);
        var countCdiEvents = _dbFixture.SharedDbContext.CdiEventForPayments.Count();
        var request = new AddCdiEventForPayment(expectedCdiEvent);

        //Act
        var subject = CreateSubject();
        var actual = await subject.Handle(request, default);

        //Assert
        Assert.Equal(expectedCdiEvent, actual.CdiEventForPayment);
        Assert.True(actual.IsNew);
        Assert.Equal(countCdiEvents + 1, _dbFixture.SharedDbContext.CdiEventForPayments.Count());
    }
}
