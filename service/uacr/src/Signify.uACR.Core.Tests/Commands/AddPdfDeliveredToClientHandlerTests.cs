using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using PdfDeliveredToClient = UacrEvents.PdfDeliveredToClient;
using PdfEntity = Signify.uACR.Core.Data.Entities.PdfDeliveredToClient;

namespace Signify.uACR.Core.Tests.Commands;

public sealed class AddPdfDeliveredToClientHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();
    private readonly IMapper _mapper = A.Fake<IMapper>();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
        => _dbFixture.DisposeAsync();

    private AddPdfDeliveredToClientHandler CreateSubject()
        => new(A.Dummy<ILogger<AddPdfDeliveredToClientHandler>>(),
            _dbFixture.SharedDbContext, _mapper);

    [Fact]
    public async Task Handle_WhereMatchingEntityFound_DoesNotAddAnother()
    {
        const long evaluationId = 10;
        var eventId = Guid.NewGuid();

        var expected = new PdfEntity
        {
            EvaluationId = evaluationId,
            EventId = eventId
        };

        await _dbFixture.SharedDbContext.PdfDeliveredToClients.AddAsync(expected);
        await _dbFixture.SharedDbContext.SaveChangesAsync();

        var countExisting = _dbFixture.SharedDbContext.PdfDeliveredToClients.Count();

        var subject = CreateSubject();

        await subject.Handle(new AddPdfDeliveredToClient(new PdfDeliveredToClient
        {
            EvaluationId = evaluationId,
            EventId = eventId
        }), default);

        A.CallTo(() => _mapper.Map<PdfEntity>(A<PdfDeliveredToClient>._))
            .MustNotHaveHappened();

        Assert.Equal(countExisting, _dbFixture.SharedDbContext.PdfDeliveredToClients.Count());
    }

    [Fact]
    public async Task Handle_WithNewPdfDeliveredToClient_AddsToDb()
    {
        const long evaluationId = 10;
        var eventId = Guid.NewGuid();

        var expected = new PdfEntity
        {
            EvaluationId = evaluationId,
            EventId = eventId
        };

        A.CallTo(() => _mapper.Map<PdfEntity>(A<PdfDeliveredToClient>._))
            .Returns(expected);

        var countExisting = _dbFixture.SharedDbContext.PdfDeliveredToClients.Count();

        var subject = CreateSubject();

        await subject.Handle(new AddPdfDeliveredToClient(new PdfDeliveredToClient
        {
            EvaluationId = evaluationId,
            EventId = eventId
        }), default);

        A.CallTo(() => _mapper.Map<PdfEntity>(A<PdfDeliveredToClient>._))
            .MustHaveHappenedOnceExactly();

        Assert.Equal(countExisting + 1, _dbFixture.SharedDbContext.PdfDeliveredToClients.Count());
    }
}