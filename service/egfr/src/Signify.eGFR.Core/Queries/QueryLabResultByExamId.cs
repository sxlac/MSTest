using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;

namespace Signify.eGFR.Core.Queries;

[ExcludeFromCodeCoverage]
public class QueryLabResultByExamId(int examId) : IRequest<LabResult>
{
    public int ExamId { get; } = examId;
}

public class QueryLabResultByExamIdHandler(DataContext egfrDataContext)
    : IRequestHandler<QueryLabResultByExamId, LabResult>
{
    public async Task<LabResult> Handle(QueryLabResultByExamId request, CancellationToken cancellationToken)
    {
        return await egfrDataContext.LabResults
            .AsNoTracking()
            .FirstOrDefaultAsync(
                exam => exam.ExamId == request.ExamId,
                cancellationToken);
    }
}