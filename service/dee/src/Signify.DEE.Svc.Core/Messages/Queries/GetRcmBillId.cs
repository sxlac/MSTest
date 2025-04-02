using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetRcmBillId(int examId) : IRequest<string>
{
    public int ExamId { get; set; } = examId;
}

public class GetRcmBillIdHandler(ILogger<GetRcmBillIdHandler> log, DataContext context)
    : IRequestHandler<GetRcmBillId, string>
{
    [Trace]
    public async Task<string> Handle(GetRcmBillId request, CancellationToken cancellationToken)
    {
        log.LogDebug("{request} -- RCM Billid lookup", request);

        var rcmBill = await context.DEEBilling.FirstOrDefaultAsync(e => e.ExamId == request.ExamId, cancellationToken);

        if (rcmBill != null)
        {
            log.LogDebug("ExamId: {ExamId} RCMBill ID record found", request.ExamId);
            return rcmBill.BillId;
        }

        log.LogDebug("ExamId: {ExamId} --  RCMBill ID record not found", request.ExamId);
        return string.Empty;
    }
}