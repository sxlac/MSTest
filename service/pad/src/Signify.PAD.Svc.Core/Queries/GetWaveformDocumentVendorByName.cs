using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;

namespace Signify.PAD.Svc.Core.Queries
{
    public class GetWaveformDocumentVendorByName : IRequest<WaveformDocumentVendor>
	{
		public string WaveformDocumentVendorName { get; init; }
	}

    /// <summary>
    /// Get WaveformDocumentVendor details from database by WaveformDocumentVendorName.
    /// </summary>
    public class GetWaveformDocumentVendorByNameHandler : IRequestHandler<GetWaveformDocumentVendorByName, WaveformDocumentVendor>
	{
		private readonly PADDataContext _context;
		private readonly ILogger _logger;

		public GetWaveformDocumentVendorByNameHandler(PADDataContext context, ILogger<GetWaveformDocumentVendorByNameHandler> logger)
		{
            _context = context;
			_logger = logger;
		}

		[Trace]
		public async Task<WaveformDocumentVendor> Handle(GetWaveformDocumentVendorByName request, CancellationToken cancellationToken)
		{
			try
			{
                return await _context.WaveformDocumentVendor.AsNoTracking().FirstOrDefaultAsync(x => x.VendorName == request.WaveformDocumentVendorName, cancellationToken);
            }
			catch (Exception e)
			{
                _logger.LogError(e, "Error retrieving WaveformDocumentVendor due to error {e.Message}", e.Message);
            }
            return await Task.FromResult<WaveformDocumentVendor>(null);
        }
	}
}
