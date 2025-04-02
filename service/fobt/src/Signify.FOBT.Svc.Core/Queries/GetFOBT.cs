using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Queries
{
    public class GetFOBT : IRequest<Data.Entities.FOBT>
    {
        public long EvaluationId { get; set; }
    }

    /// <summary>
    /// Get FOBT details from database.
    /// </summary>
    public class GetFOBTHandler : IRequestHandler<GetFOBT, Data.Entities.FOBT>
    {
        private readonly FOBTDataContext _dataContext;
        private readonly ILogger<GetFOBTHandler> _logger;

        public GetFOBTHandler(FOBTDataContext dataContext, ILogger<GetFOBTHandler> logger)
        {
            _dataContext = dataContext;
            _logger = logger;
        }

        [Trace]
        public async Task<Data.Entities.FOBT> Handle(GetFOBT request, CancellationToken cancellationToken)
        {
            try
            {
                return await _dataContext.FOBT.AsNoTracking().FirstOrDefaultAsync(s => s.EvaluationId == request.EvaluationId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving FOBTs: {@ex}", ex);
                return null;
            }
        }
    }
}