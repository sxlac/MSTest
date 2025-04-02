using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;

namespace Signify.eGFR.Core.Queries;

public class QueryExamByCenseoId(string censeoId, DateTimeOffset? collectionDate) : IRequest<Exam>
{
    public string CenseoId { get; } = censeoId;
    public DateTimeOffset? CollectionDate { get; } = collectionDate;
}

public class QueryExamByCenseoIdHandler(DataContext egfrDataContext) : IRequestHandler<QueryExamByCenseoId, Exam>
{
    public async Task<Exam> Handle(QueryExamByCenseoId request, CancellationToken cancellationToken)
    {
        return await egfrDataContext.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(
                each => (each.CenseoId == request.CenseoId && each.DateOfService == request.CollectionDate),
                cancellationToken)
            .ConfigureAwait(false);
    }
}