using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.EventHandlers;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Filters;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers
{
    public class PdfDeliveredHandlerTest
    {
        private readonly IProductFilter _productFilter = A.Fake<IProductFilter>();
        private readonly IMediator _mediator = A.Fake<IMediator>();
        private readonly IMapper _mapper = A.Fake<IMapper>();
        private readonly TestableEndpointInstance _messageSessionInstance = new();
        private readonly PdfDeliveredHandler _pdfDeliveredHandler;
        private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();
        
        public PdfDeliveredHandlerTest()
        {
            var logger = A.Dummy<ILogger<PdfDeliveredHandler>>();
            
            _pdfDeliveredHandler = new PdfDeliveredHandler(logger, _productFilter, _messageSessionInstance, _observabilityService);
        }

        [Fact]
        public async Task PdfDeliveredHandler_WhenProductCodeIsNotCKD()
        {
            var @event = new PdfDeliveredToClient();

            A.CallTo(() => _productFilter.ShouldProcess(A<IEnumerable<string>>._))
                .Returns(false);

            await _pdfDeliveredHandler.Handle(@event, CancellationToken.None);
            _messageSessionInstance.SentMessages.Length.Should().Be(0);
        }

        [Fact]
        public async Task PdfDeliveredHandler_PublishCheck()
        {
            var @event = GetPdfDeliveredToClientWitCKD();

            A.CallTo(() => _productFilter.ShouldProcess(A<IEnumerable<string>>._))
                .Returns(true);

            A.CallTo(() => _mapper.Map<CreateOrUpdatePDFToClient>(A<PdfDeliveredToClient>._)).Returns(GetCreateOrUpdatePdfToClient);
            A.CallTo(() => _mediator.Send(A<CreateOrUpdatePDFToClient>._, CancellationToken.None)).Returns(default(PDFToClient));
            A.CallTo(() => _mediator.Send(A<CreateCKDStatus>._, CancellationToken.None)).Returns(default(CKDStatus));

            await _pdfDeliveredHandler.Handle(@event, CancellationToken.None);
            _messageSessionInstance.SentMessages.Length.Should().Be(1);

            A.CallTo(() => _productFilter.ShouldProcess(A<IEnumerable<string>>.That.Matches(e =>
                    e.Contains("CKD"))))
                .MustHaveHappened();
        }

        private static PdfDeliveredToClient GetPdfDeliveredToClientWitCKD()
        {
            var @event = new PdfDeliveredToClient()
            {
                BatchId = 678,
                BatchName = "c0b2f2d9-d39b-4293-ab88-532b5412dc4f",
                CreatedDateTime = DateTime.UtcNow,
                DeliveryDateTime = DateTime.UtcNow,
                EvaluationId = 324357,
                EventId = new Guid("c0b2f2d9-d39b-4293-ab88-532b5412dc4f"),
                ProductCodes = new List<string>() { "HHRA", "CKD" }
            };
            return @event;
        }

        private static CreateOrUpdatePDFToClient GetCreateOrUpdatePdfToClient => new CreateOrUpdatePDFToClient
        {
            BatchId = 678,
            BatchName = "c0b2f2d9-d39b-4293-ab88-532b5412dc4f",
            CKDId = 2,
            DeliveryDateTime = DateTime.UtcNow,
            EvaluationId = 324357,
            EventId = new Guid("c0b2f2d9-d39b-4293-ab88-532b5412dc4f"),
            DeliveryCreatedDateTime = DateTime.UtcNow,
            PDFDeliverId = 321
        };
    }
}
