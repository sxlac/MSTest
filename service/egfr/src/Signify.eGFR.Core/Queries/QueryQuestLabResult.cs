using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;

namespace Signify.eGFR.Core.Queries;

public class QueryQuestLabResult(string censeoId, DateTimeOffset? collectionDate) : IRequest<QuestLabResult>
{
    public string CenseoId { get; } = censeoId;
    public DateTimeOffset? CollectionDate { get; } = collectionDate;
}

public class QueryQuestLabResultHandler(DataContext egfrDataContext)
    : IRequestHandler<QueryQuestLabResult, QuestLabResult>
{
    public async Task<QuestLabResult> Handle(QueryQuestLabResult request, CancellationToken cancellationToken)
        => await egfrDataContext.QuestLabResults
            .AsNoTracking()
            .FirstOrDefaultAsync(
                each => (each.CenseoId == request.CenseoId && each.CollectionDate == request.CollectionDate),
                cancellationToken)
            .ConfigureAwait(false);
}