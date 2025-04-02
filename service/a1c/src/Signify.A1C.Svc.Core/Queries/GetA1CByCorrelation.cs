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
	public class GetA1CByCorrelation : IRequest<Data.Entities.A1C>
	{
		public Guid OrderCorrelationId { get; set; }
	}

	/// <summary>
	/// Get A1C details from database.
	/// </summary>
	public class GetA1CByCorrelationHandler : IRequestHandler<GetA1CByCorrelation, Data.Entities.A1C>
	{
		private readonly A1CDataContext _dataContext;
		private readonly ILogger<GetA1CByCorrelationHandler> _logger;
		public GetA1CByCorrelationHandler(A1CDataContext dataContext, ILogger<GetA1CByCorrelationHandler> logger)
		{
			_dataContext = dataContext;
			_logger = logger;
		}

		[Trace]
		public async Task<Data.Entities.A1C> Handle(GetA1CByCorrelation request, CancellationToken cancellationToken)
		{
			try
			{
				var a1C = await _dataContext.A1C.AsNoTracking().FirstOrDefaultAsync(s => s.OrderCorrelationId == request.OrderCorrelationId, cancellationToken: cancellationToken);
				return await Task.FromResult(a1C);
			}
			catch (Exception ex)
			{
				_logger.LogError("Error retrieving A1Cs by OrderCorreclationId : {@ex}", ex);
				return null;
			}
		}
	}
}
