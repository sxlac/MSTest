using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.CKD.Svc.Core.Queries
{
    public class GetPdfToClient : IRequest<PDFToClient>
    {
        public long EvaluationId { get; set; }
    }

    public class GetPdfToClientHandler : IRequestHandler<GetPdfToClient, PDFToClient>
    {
        private readonly CKDDataContext _dataContext;

        public GetPdfToClientHandler(CKDDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public Task<PDFToClient> Handle(GetPdfToClient request, CancellationToken cancellationToken)
        {
            return _dataContext.PDFToClient.AsNoTracking().FirstOrDefaultAsync(x => x.EvaluationId == request.EvaluationId, cancellationToken);
        }
    }
}
