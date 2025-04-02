using AutoMapper;
using FakeItEasy;
using MediatR;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Events;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Commands;

public class InsertPdfToClientTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly InsertPdfToClientHandler _handler;

    public InsertPdfToClientTests()
    {
        _handler = new InsertPdfToClientHandler(_mapper, _mediator);
    }

    [Fact]
    public async Task Handler_HappyPath_Test()
    {
        // Arrange
        var insertPdfToClientTransactionCommand = GetInsertPdfToClientTransactionCommand();
        A.CallTo(() => _mapper.Map<CreateOrUpdatePDFToClient>(A<PdfDeliveredToClient>._)).Returns(GetMappedCreateOrUpdatePdfToClient());

        // Act
        await _handler.Handle(insertPdfToClientTransactionCommand, default);

        // Assert
        A.CallTo(() => _mapper.Map<CreateOrUpdatePDFToClient>(A<PdfDeliveredToClient>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePDFToClient>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateFOBTStatus>._, A<CancellationToken>._)).MustHaveHappened();
    }

    private static InsertPdfToClient GetInsertPdfToClientTransactionCommand()
    {
        return new InsertPdfToClient
        {
            PdfDeliveredToClient = BuildPdfDeliveredToClient(),
            Fobt = Mocks.Models.FobtEntityMock.BuildFobt()
        };
    }

    private static PdfDeliveredToClient BuildPdfDeliveredToClient()
    {
        return new PdfDeliveredToClient 
        { 
            EventId = Guid.NewGuid(),
            EvaluationId = 123456,
            ProductCodes = ["FOBT", "A1C"],
            DeliveryDateTime = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow.AddMinutes(-1),
            BatchId = 123456789,
            BatchName = string.Empty
        };
    }

    private static CreateOrUpdatePDFToClient GetMappedCreateOrUpdatePdfToClient()
    {
        return new CreateOrUpdatePDFToClient
        { 
            EventId = Guid.NewGuid()
        };
    }
}