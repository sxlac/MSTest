using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;

namespace Signify.PAD.Svc.Core.Queries
{
	public class GetWaveformDocumentByFilename : IRequest<WaveformDocument>
	{
		public string Filename { get; init; }
	}

	/// <summary>
	/// Get WaveformDocument details from database by Filename.
	/// </summary>
	public class GetWaveformDocumentByFilenameHandler : IRequestHandler<GetWaveformDocumentByFilename, WaveformDocument>
	{
		private readonly PADDataContext _context;

		public GetWaveformDocumentByFilenameHandler(PADDataContext context)
		{
			_context = context;
		}

		[Trace]
		public Task<WaveformDocument> Handle(GetWaveformDocumentByFilename request, CancellationToken cancellationToken)
		{
			return _context.WaveformDocument
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Filename == request.Filename, cancellationToken);
		}
	}
}
