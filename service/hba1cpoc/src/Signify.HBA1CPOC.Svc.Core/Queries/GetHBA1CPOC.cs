using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.HBA1CPOC.Svc.Core.Data;
using System.Threading;
using System.Threading.Tasks;
using HbA1cPoc = Signify.HBA1CPOC.Svc.Core.Data.Entities.HBA1CPOC;

namespace Signify.HBA1CPOC.Svc.Core.Queries
{
	public class GetHBA1CPOC : IRequest<HbA1cPoc>
	{
		public long EvaluationId { get; set; }

		/// <summary>
		/// Whether to include statuses with the entity
		/// </summary>
		public bool IncludeStatuses { get; set; }
	}

	public class GetHBA1CPOCHandler : IRequestHandler<GetHBA1CPOC, HbA1cPoc>
	{
		private readonly Hba1CpocDataContext _dataContext;

		public GetHBA1CPOCHandler(Hba1CpocDataContext dataContext)
		{
			_dataContext = dataContext;
		}

		[Trace]
		public Task<HbA1cPoc> Handle(GetHBA1CPOC request, CancellationToken cancellationToken)
		{
			var queryable = _dataContext.HBA1CPOC
				.AsNoTracking();

			if (request.IncludeStatuses)
				queryable = queryable.Include(h => h.ExamStatuses);

			return queryable.FirstOrDefaultAsync(h => h.EvaluationId == request.EvaluationId, cancellationToken);
		}
	}
}
