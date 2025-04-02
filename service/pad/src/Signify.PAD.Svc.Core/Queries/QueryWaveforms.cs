using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Queries;

/// <summary>
/// Query to find all waveforms from the database that were created within a given timeframe
/// </summary>
public class QueryWaveforms : IRequest<ICollection<WaveformDocument>>
{
    /// <summary>
    /// Name of the vendor the waveform is from
    /// </summary>
    public string VendorName { get; }
    /// <summary>
    /// Starting timestamp to search for when the waveforms were saved
    /// </summary>
    /// <remarks>Inclusive</remarks>
    public DateTime StartDateTime { get; }
    /// <summary>
    /// Ending timestamp to search for when the waveforms were saved
    /// </summary>
    /// <remarks>Inclusive</remarks>
    public DateTime EndDateTime { get; }

    public QueryWaveforms(string vendorName, DateTime startDateTime, DateTime endDateTime)
    {
        VendorName = vendorName;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
    }
}

public class QueryWaveformsHandler : IRequestHandler<QueryWaveforms, ICollection<WaveformDocument>>
{
    private readonly ILogger _logger;
    private readonly PADDataContext _context;

    public QueryWaveformsHandler(ILogger<QueryWaveformsHandler> logger, PADDataContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<ICollection<WaveformDocument>> Handle(QueryWaveforms request, CancellationToken cancellationToken)
    {
        var documents = await _context.WaveformDocument
            .AsTracking() // these will likely all be removed, so track them
            .Include(each => each.WaveformDocumentVendor)
            .Where(each => each.WaveformDocumentVendor.VendorName == request.VendorName && each.CreatedDateTime >= request.StartDateTime && each.CreatedDateTime <= request.EndDateTime)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} waveforms in the database for Vendor={Vendor} created between {StartDateTime} and {EndDateTime}",
            documents.Count, request.VendorName, request.StartDateTime, request.EndDateTime);

        return documents;
    }
}
