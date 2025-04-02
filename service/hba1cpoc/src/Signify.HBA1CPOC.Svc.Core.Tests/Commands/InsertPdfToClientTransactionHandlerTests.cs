using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.StaticEntity;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Commands;

public class InsertPdfToClientTransactionHandlerTests
{
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly InsertPdfToClientTransactionHandler _handler;
        
    public InsertPdfToClientTransactionHandlerTests()
    {
        _handler = new InsertPdfToClientTransactionHandler(A.Dummy<ILogger<InsertPdfToClientTransactionHandler>>(), _mapper, _mediator);
    }

    [Fact]
    public async Task InsertPdfToClientTransactionHandle_SendCorrectRequest_CreatePdfToClientAndStatus()
    {
        // Arrange
        var request = GetInsertPdfToClientTransaction();
        A.CallTo(() => _mapper.Map<CreateOrUpdatePDFToClient>(A<PdfDeliveredToClient>._)).Returns(StaticMockEntities.BuildCreateOrUpdatePDFToClient);

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mapper.Map<CreateOrUpdatePDFToClient>(A<PdfDeliveredToClient>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePDFToClient>._, default)).MustHaveHappened();
    }

    private static InsertPdfToClientTransaction GetInsertPdfToClientTransaction()
    {
        return new InsertPdfToClientTransaction
        { 
            PdfDeliveredToClient = BuildPdfDeliveredToClient(),
            HbA1cPoc = StaticMockEntities.Hba1Cpoc
        };
    }

    private static PdfDeliveredToClient BuildPdfDeliveredToClient()
    {
        return new PdfDeliveredToClient
        { 
            EventId = Guid.NewGuid(),
            EvaluationId = 123456,
            ProductCodes= ["FOBT", ApplicationConstants.ProductCode],
            DeliveryDateTime = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow.AddMinutes(-1),
            BatchId = 123456789,
            BatchName = string.Empty
        };
    }
}