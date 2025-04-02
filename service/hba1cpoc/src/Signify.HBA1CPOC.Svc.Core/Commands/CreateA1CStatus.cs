using MediatR;
using NewRelic.Api.Agent;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.Commands
{
    public class CreateHBA1CPOCStatus : IRequest<HBA1CPOCStatus>
	{
		public int StatusCodeId { get; set; }
		public int HBA1CPOCId { get; set; }

	}

	public class CreateHBA1CPOCStatusHandler : IRequestHandler<CreateHBA1CPOCStatus, HBA1CPOCStatus>
	{
		private readonly Hba1CpocDataContext _context;
		private readonly IApplicationTime _applicationTime;

		public CreateHBA1CPOCStatusHandler(Hba1CpocDataContext context, IApplicationTime applicationTime)
		{
			_context = context;
			_applicationTime = applicationTime;
		}
		
		[Trace]
		public async Task<HBA1CPOCStatus> Handle(CreateHBA1CPOCStatus request, CancellationToken cancellationToken)
		{
			var HBA1CPOCStatus = new HBA1CPOCStatus()
			{
				HBA1CPOCStatusCodeId = request.StatusCodeId,
				HBA1CPOCId = request.HBA1CPOCId,
				CreatedDateTime = _applicationTime.UtcNow()
			};
			
			var logStatus = await _context.HBA1CPOCStatus.AddAsync(HBA1CPOCStatus, cancellationToken);
			await _context.SaveChangesAsync(cancellationToken);

			return logStatus.Entity;
		}
	}
}
