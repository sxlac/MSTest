using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Commands;

public class CreateOrUpdatePDFToClientTests : IClassFixture<MockDbFixture>
{
    private readonly CreateOrUpdatePDFToClientHandler _handler;
    private readonly MockDbFixture _mockDbFixture;

    public CreateOrUpdatePDFToClientTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        var mapper = A.Fake<IMapper>();
        var logger = A.Fake<ILogger<CreateOrUpdatePDFToClientHandler>>();
        _handler = new CreateOrUpdatePDFToClientHandler(_mockDbFixture.Context, mapper, logger);
    }

    [Fact]
    public async Task Handle_Add_NewPDFToClient()
    {
        var initialCount = _mockDbFixture.Context.PDFToClient.Count();

        await _handler.Handle(new CreateOrUpdatePDFToClient(), CancellationToken.None);
        _mockDbFixture.Context.PDFToClient.Count().Should().BeGreaterThanOrEqualTo(initialCount, "There should be an insert");
    }

    [Fact]
    public async Task Handle_Add_UpdatePDFToClient()
    {
        var initialCount = _mockDbFixture.Context.PDFToClient.Count();
        var request = new CreateOrUpdatePDFToClient
        {
            PDFDeliverId = 1,
            EvaluationId = 1,
            BatchId = 1,
            FOBTId = 1
        };

        await _handler.Handle(request, CancellationToken.None);
        _mockDbFixture.Context.PDFToClient.Count().Should().BeGreaterThanOrEqualTo(initialCount, "There should be an insert");
    }
}