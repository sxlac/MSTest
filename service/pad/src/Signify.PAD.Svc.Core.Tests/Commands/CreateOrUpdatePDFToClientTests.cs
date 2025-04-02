using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class CreateOrUpdatePDFToClientTests : IClassFixture<MockDbFixture>
{
    private readonly MockDbFixture _mockDbFixture;
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly CreateOrUpdatePDFToClientHandler _handler;

    public CreateOrUpdatePDFToClientTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _handler = new CreateOrUpdatePDFToClientHandler( mockDbFixture.Context, _mapper, A.Dummy<ILogger<CreateOrUpdatePDFToClientHandler>>());
    }

    [Fact]
    public async Task Should_Create_PadPdfToClient_DataCheck()
    {
        A.CallTo(() => _mapper.Map<PDFToClient>(A<CreateOrUpdatePDFToClient>._)).Returns(CreatePdfToClient);
        var result = await _handler.Handle(CreateOrUpdatePdfToClient, CancellationToken.None);
        _mockDbFixture.Context.PDFToClient.Any(x => x.BatchName == result.BatchName).Should().BeTrue();
    }
    
    [Fact]
    public async Task Should_Create_PadPdfToClient_CountTest()
    {
        A.CallTo(() => _mapper.Map<PDFToClient>(A<CreateOrUpdatePDFToClient>._)).Returns(CreatePdfToClient);
        var initialCount = _mockDbFixture.Context.PDFToClient.Count();
        await _handler.Handle(CreateOrUpdatePdfToClient, CancellationToken.None);
        _mockDbFixture.Context.PDFToClient.Count().Should().BeGreaterThan(initialCount, "There shd be an insert");
    }
    
    [Fact]
    public async Task Should_Create_PadPdfToClient()
    {
        A.CallTo(() => _mapper.Map<PDFToClient>(A<CreateOrUpdatePDFToClient>._)).Returns(CreatePdfToClient);
        var initialCount = _mockDbFixture.Context.PDFToClient.Count();
        await _handler.Handle(CreateOrUpdatePdfToClient, CancellationToken.None);
        _mockDbFixture.Context.PDFToClient.Count().Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public async Task Should_Create_PadPdfToClient_TypeCheck()
    {
        A.CallTo(() => _mapper.Map<PDFToClient>(A<CreateOrUpdatePDFToClient>._)).Returns(CreatePdfToClient);
        var result = await _handler.Handle(CreateOrUpdatePdfToClient, CancellationToken.None);
        result.Should().BeOfType<PDFToClient>();
    }

    [Fact]
    public async Task Should_Update_PadPdfToClient()
    {
        A.CallTo(() => _mapper.Map<PDFToClient>(A<CreateOrUpdatePDFToClient>.That.Matches(e => e.PADId == 0))).Returns(CreatePdfToClient);
        
        A.CallTo(() => _mapper.Map<PDFToClient>(A<CreateOrUpdatePDFToClient>._)).Returns(CreatePdfToClient);
        var before = await _handler.Handle(CreateOrUpdatePdfToClient, CancellationToken.None);

        var updated = CreateOrUpdatePdfToClient;
        updated.PDFDeliverId = before.PDFDeliverId;
        var updatePad = CreatePdfToClient;
        updatePad.BatchName = "updated";
        updatePad.PDFDeliverId = before.PDFDeliverId;

        _mockDbFixture.Context.ChangeTracker.Clear();

        A.CallTo(() => _mapper.Map<PDFToClient>(A<CreateOrUpdatePDFToClient>.That.Matches(e => e.PADId == before.PADId))).Returns(updatePad);

        var after = await _handler.Handle(updated, CancellationToken.None);
        Assert.Equal(before.PADId, after.PADId);
        Assert.NotEqual(before.BatchName, after.BatchName);
    }
       
    private static PDFToClient CreatePdfToClient => new()
    {
        EventId = Guid.NewGuid().ToString(),
        EvaluationId = 1,
        DeliveryDateTime = DateTime.Now,
        DeliveryCreatedDateTime = DateTime.Now,
        BatchId = 1,
        BatchName = "Batch123",
        PADId = 1,
        CreatedDateTime = DateTime.Now
    };
    
    private static CreateOrUpdatePDFToClient CreateOrUpdatePdfToClient => new()
    {
        EventId = Guid.NewGuid(),
        EvaluationId = 1,
        DeliveryDateTime = DateTime.Now,
        DeliveryCreatedDateTime = DateTime.Now,
        BatchId = 1,
        BatchName = "Batch123",
        PADId = 1
    };
}