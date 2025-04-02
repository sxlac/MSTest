using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Queries
{
    public class GetFobtByFobtId : IRequest<Data.Entities.FOBT>
    {
        public int FobtId { get; set; }
    }

    public class GetFobtByFobtIdHandler : IRequestHandler<GetFobtByFobtId, Data.Entities.FOBT>
    {
        private readonly FOBTDataContext _dataContext;
        private readonly ILogger<GetFobtByFobtIdHandler> _logger;

        public GetFobtByFobtIdHandler(FOBTDataContext dataContext, ILogger<GetFobtByFobtIdHandler> logger)
        {
            _dataContext = dataContext;
            _logger = logger;
        }

        public async Task<Data.Entities.FOBT> Handle(GetFobtByFobtId request, CancellationToken cancellationToken)
        {
            try
            {
                return await _dataContext.FOBT.AsNoTracking().FirstOrDefaultAsync(x => x.FOBTId == request.FobtId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving FOBT Record by Fobt Id:{FobtId}", request.FobtId);
            }

            return null;
        }
    }
}
