using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Tests.Mocks.StaticEntity;
using Signify.CKD.Svc.Core.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signify.CKD.Svc.Core.FeatureFlagging;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Commands;

public class InsertPdfToClientTransactionHandlerTests : IClassFixture<EntityFixtures>, IClassFixture<MockDbFixture>
{
    private readonly ILogger<InsertPdfToClientTransactionHandler> _logger;
    private readonly MockDbFixture _mockDbFixture;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly InsertPdfToClientTransactionHandler _handler;
    private readonly IFeatureFlags _featureFlags;

    public InsertPdfToClientTransactionHandlerTests(MockDbFixture mockDbFixture)
    {
        _mapper = A.Fake<IMapper>();
        _logger = A.Fake<ILogger<InsertPdfToClientTransactionHandler>>();
        _mediator = A.Fake<IMediator>();
        _mockDbFixture = mockDbFixture;
        _featureFlags = A.Fake<IFeatureFlags>();

        _handler = new InsertPdfToClientTransactionHandler(_logger, _mockDbFixture.Context, _mapper, _mediator, _featureFlags);
    }

    [Fact]
    public async Task InsertPdfToClientTransactionHandle_SendCorrectRequest_CreatePdfToClientAndStatus()
    {
        // Arrange
        var request = GetInsertPdfToClientTransaction();
        A.CallTo(() => _mapper.Map<CreateOrUpdatePDFToClient>(A<PdfDeliveredToClient>._)).Returns(StaticMockEntities.BuildCreateOrUpdatePDFToClient);
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePDFToClient>._, default));
        A.CallTo(() => _mediator.Send(A<CreateCKDStatus>._, default));

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        A.CallTo(() => _mapper.Map<CreateOrUpdatePDFToClient>(A<PdfDeliveredToClient>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateOrUpdatePDFToClient>._, default)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateCKDStatus>._, default)).MustNotHaveHappened();
    }

    private static InsertPdfToClientTransaction GetInsertPdfToClientTransaction()
    {
        return new InsertPdfToClientTransaction
        {
            PdfDeliveredToClient = BuildPdfDeliveredToClient(),
            CKDId = StaticMockEntities.CKD.CKDId
        };
    }

    private static PdfDeliveredToClient BuildPdfDeliveredToClient()
    {
        return new PdfDeliveredToClient
        {
            EventId = Guid.NewGuid(),
            EvaluationId = 123456,
            ProductCodes = new List<string> { "FOBT", "CKD" },
            DeliveryDateTime = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow.AddMinutes(-1),
            BatchId = 123456789,
            BatchName = string.Empty
        };
    }
}