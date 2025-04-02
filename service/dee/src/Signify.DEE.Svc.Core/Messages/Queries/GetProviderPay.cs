using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetProviderPayId(int examId) : IRequest<string>
{
    public int ExamId { get; set; } = examId;
}

public class GetProviderPayIdHandler(ILogger<GetProviderPayIdHandler> logger, DataContext context)
    : IRequestHandler<GetProviderPayId, string>
{
    [Transaction]
    public async Task<string> Handle(GetProviderPayId request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Start search for ProviderPay for ExamId={ExamId}", request.ExamId);

        var providerPay = await context.ProviderPays.FirstOrDefaultAsync(e => e.ExamId == request.ExamId, cancellationToken);

        if (providerPay != null)
        {
            logger.LogDebug("ExamId: {ExamId}, ProviderPay record found", request.ExamId);
            return providerPay.PaymentId;
        }

        logger.LogDebug("ExamId: {ExamId}, ProviderPay record not found", request.ExamId);
        return string.Empty;
    }
}