using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Commands;

/// <summary>
/// Command to delete status(es) for a given exam
/// </summary>
public class DeleteStatuses : IRequest
{
    public int PadId { get; }

    public IReadOnlyCollection<int> StatusCodeIds { get; }

    /// <exception cref="NotSupportedException">Thrown if deletion of a status code is not supported/allowed</exception>
    /// <exception cref="InvalidOperationException">Thrown if no status codes are supplied</exception>
    public DeleteStatuses(int padId, IEnumerable<StatusCodes> statusCodes)
    {
        PadId = padId;

        var hs = new HashSet<int>();

        foreach (var statusCode in statusCodes)
        {
            switch (statusCode)
            {
                // We shouldn't allow any other statuses to be deleted
                case StatusCodes.WaveformDocumentDownloaded:
                case StatusCodes.WaveformDocumentUploaded:
                    hs.Add((int)statusCode);
                    continue;
                default:
                    throw new NotSupportedException($"Deleting status code {statusCode} is not supported.");
            }
        }

        if (hs.Count < 1)
            throw new InvalidOperationException("There are no status codes to delete.");

        StatusCodeIds = hs;
    }
}

public class DeleteStatusesHandler : IRequestHandler<DeleteStatuses>
{
    private readonly ILogger _logger;
    private readonly PADDataContext _context;

    public DeleteStatusesHandler(ILogger<DeleteStatusesHandler> logger, PADDataContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task Handle(DeleteStatuses request, CancellationToken cancellationToken)
    {
        // Unfortunately we can't just use .Contains like we have below in this .Where query because
        // it's unable to translate that LINQ into SQL, so we must do client-side filtering. This
        // shouldn't be too bad, though, as we're at least able to filter by PADId server-side so
        // there shouldn't be too many statuses.
        var allStatuses = await _context.PADStatus
            .AsTracking() // we're going to remove them, so track them
            .Where(each => each.PADId == request.PadId)
            .ToListAsync(cancellationToken);

        allStatuses.RemoveAll(each => !request.StatusCodeIds.Contains(each.PADStatusCodeId));

        var countDeleted = 0;

        if (allStatuses.Count > 1)
        {
            _context.PADStatus.RemoveRange(allStatuses);

            countDeleted = await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Deleted {CountDeleted} statuses for PadId={PadId} with status code in {StatusCodes}",
            countDeleted, request.PadId, string.Join(',', request.StatusCodeIds.Select(each => (StatusCodes)each)));
    }
}
