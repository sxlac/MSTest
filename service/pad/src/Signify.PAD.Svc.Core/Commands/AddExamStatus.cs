using MediatR;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;

namespace Signify.PAD.Svc.Core.Commands;

/// <summary>
/// Command to add a <see cref="PADStatus"/> record to the database
/// </summary>
public class AddExamStatus : IRequest<AddExamStatusResponse>
{
    public PADStatus Status { get; }

    public AddExamStatus(PADStatus status)
    {
        Status = status;
    }
}

/// <summary>
/// Response to the <see cref="AddExamStatus"/> command
/// </summary>
[ExcludeFromCodeCoverage]
public class AddExamStatusResponse
{
    public PADStatus Status { get; }

    public AddExamStatusResponse(PADStatus status)
    {
        Status = status;
    }
}

public class AddExamStatusHandler : IRequestHandler<AddExamStatus, AddExamStatusResponse>
{
    private readonly PADDataContext _context;

    public AddExamStatusHandler(PADDataContext context)
    {
        _context = context;
    }

    public async Task<AddExamStatusResponse> Handle(AddExamStatus request, CancellationToken cancellationToken)
    {
        var entity = (await _context.PADStatus.AddAsync(request.Status, cancellationToken)).Entity;

        await _context.SaveChangesAsync(cancellationToken);

        return new AddExamStatusResponse(entity);
    }
}