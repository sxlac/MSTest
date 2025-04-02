using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Signify.FOBT.Svc.Core.Messages.Queries;

[ExcludeFromCodeCoverage]
public class GetPDFToClient : IRequest<PDFToClient>
{
    public GetPDFToClient(int fobtId, int evaluationId)
    {
        FOBTId = fobtId;
        EvaluationId = evaluationId;
    }

    public int FOBTId { get; set; }
    public int EvaluationId { get; set; }
}

public class GetPDFToClientHandler : IRequestHandler<GetPDFToClient, PDFToClient>
{
    private readonly FOBTDataContext _context;

    public GetPDFToClientHandler(FOBTDataContext dataContext)
    {
        _context = dataContext;
    }

    [Trace]
    public Task<PDFToClient> Handle(GetPDFToClient request, CancellationToken cancellationToken)
    {
        return _context.PDFToClient.AsNoTracking().FirstOrDefaultAsync(x => x.EvaluationId == request.EvaluationId, cancellationToken);
    }
}