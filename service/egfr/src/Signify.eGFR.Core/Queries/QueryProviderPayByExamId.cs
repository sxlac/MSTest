using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.eGFR.Core.Data;

namespace Signify.eGFR.Core.Queries;

[ExcludeFromCodeCoverage]
public class QueryProviderPayByExamId : IRequest<Data.Entities.ProviderPay>
{
    public int ExamId { get; set; }
}

public class QueryProviderPayByExamIdHandler(
    ILogger<QueryProviderPayByExamIdHandler> logger,
    DataContext eGfrDataContext)
    : IRequestHandler<QueryProviderPayByExamId, Data.Entities.ProviderPay>
{
    [Transaction]
    public async Task<Data.Entities.ProviderPay> Handle(QueryProviderPayByExamId request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Start search for ProviderPay for ExamId={ExamId}", request.ExamId);

        try
        {
            var providerPay = await eGfrDataContext.ProviderPay.AsNoTracking().FirstOrDefaultAsync(s => s.ExamId == request.ExamId, cancellationToken: cancellationToken);
            return await Task.FromResult(providerPay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving ProviderPay with ExamId: {ExamId}", request.ExamId);
            throw;
        }
    }
}
