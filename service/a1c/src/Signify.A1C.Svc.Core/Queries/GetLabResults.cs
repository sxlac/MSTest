using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.A1C.Svc.Core.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;

namespace Signify.A1C.Svc.Core.Queries
{
	public class GetLabResults : IRequest<Data.Entities.LabResults>
	{
		public Guid OrderCorrelationId { get; set; }
	}

	/// <summary>
	/// Get A1C details from database.
	/// </summary>
	public class GetLabResultsHandler : IRequestHandler<GetLabResults, Data.Entities.LabResults>
	{
		private readonly A1CDataContext _dataContext;
		private readonly ILogger<GetLabResultsHandler> _logger;
		public GetLabResultsHandler(A1CDataContext dataContext, ILogger<GetLabResultsHandler> logger)
		{
			_dataContext = dataContext;
			_logger = logger;
		}

		[Trace]
		public async Task<Data.Entities.LabResults> Handle(GetLabResults request, CancellationToken cancellationToken)
		{
			try
			{
				var LabResult = await _dataContext.LabResults.AsNoTracking().FirstOrDefaultAsync(s => s.OrderCorrelationId  == request.OrderCorrelationId, cancellationToken: cancellationToken);
				return await Task.FromResult(LabResult);
			}
			catch (Exception ex)
			{
				_logger.LogError("Error retrieving LabResult by A1CId : {@ex}", ex);
				return null;
			}
		}
	}
}
