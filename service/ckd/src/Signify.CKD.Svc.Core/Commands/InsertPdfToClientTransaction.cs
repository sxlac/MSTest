using System;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Queries;
using System.Threading;
using System.Threading.Tasks;
using Signify.CKD.Svc.Core.FeatureFlagging;

namespace Signify.CKD.Svc.Core.Commands
{
    public class InsertPdfToClientTransaction : IRequest<PDFToClient>
    {
        public PdfDeliveredToClient PdfDeliveredToClient { get; set; }
        public int CKDId { get; set; }
        public long EvaluationId { get; set; }
        public bool IsPerformed { get; set; }
    }

    public class InsertPdfToClientTransactionHandler : IRequestHandler<InsertPdfToClientTransaction, PDFToClient>
    {
        private readonly ILogger<InsertPdfToClientTransactionHandler> _logger;
        private readonly CKDDataContext _dataContext;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
        private readonly IFeatureFlags _featureFlags;

        public InsertPdfToClientTransactionHandler(ILogger<InsertPdfToClientTransactionHandler> logger, CKDDataContext dataContext, IMapper mapper,
            IMediator mediator, IFeatureFlags featureFlags)
        {
            _logger = logger;
            _dataContext = dataContext;
            _mapper = mapper;
            _mediator = mediator;
            _featureFlags = featureFlags;
        }

        public async Task<PDFToClient> Handle(InsertPdfToClientTransaction request, CancellationToken cancellationToken)
        {
            var pdfDeliveryReceived = _mapper.Map<CreateOrUpdatePDFToClient>(request.PdfDeliveredToClient);
            pdfDeliveryReceived.CKDId = request.CKDId;

            var pdfEntry = await _mediator.Send(new GetPdfToClient { EvaluationId = request.EvaluationId }, cancellationToken)
                           ?? await _mediator.Send(pdfDeliveryReceived, cancellationToken);


            _logger.LogDebug("{Handler} updated {Table} table for EvaluationID:{EvaluationId}, EventId:{EventId}",
                nameof(InsertPdfToClientTransactionHandler), nameof(PDFToClient), request.PdfDeliveredToClient.EvaluationId, request.PdfDeliveredToClient.EventId);
            return pdfEntry;
        }
    }
}