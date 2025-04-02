using MediatR;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Commands;

public class CreateAoeSymptomSupportResult : IRequest<Unit>
{
    public AoeSymptomSupportResult AoeSymptomSupportResult { get; set; }
}

public class CreateAoeSymptomSupportResultHandler : IRequestHandler<CreateAoeSymptomSupportResult, Unit>
{
    private readonly PADDataContext _context;
    private readonly IApplicationTime _applicationTime;

    public CreateAoeSymptomSupportResultHandler(PADDataContext context, IApplicationTime applicationTime)
    {
        _context = context;
        _applicationTime = applicationTime;
    }

    [Trace]
    public async Task<Unit> Handle(CreateAoeSymptomSupportResult request, CancellationToken cancellationToken)
    {
        request.AoeSymptomSupportResult.CreatedDateTime = _applicationTime.UtcNow();

        await _context.AoeSymptomSupportResult.AddAsync(request.AoeSymptomSupportResult, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}