using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Queries
{
    public class GetFobtBilling : IRequest<FOBTBilling>
    {
        public int FobtId { get; set; }

        public string BillingProductCode { get; set; }

        public GetFobtBilling(int fobtId, string billingProductCode)
        {
            FobtId = fobtId;
            BillingProductCode = billingProductCode;
        }
    }

    public class GetFobtBillingHandler : IRequestHandler<GetFobtBilling, FOBTBilling>
    {
        private readonly FOBTDataContext _dataContext;
        private readonly ILogger<GetFobtBillingHandler> _logger;

        public GetFobtBillingHandler(FOBTDataContext dataContext, ILogger<GetFobtBillingHandler> logger)
        {
            _dataContext = dataContext;
            _logger = logger;
        }

        public async Task<FOBTBilling> Handle(GetFobtBilling request, CancellationToken cancellationToken)
        {
            try
            {
                return await _dataContext.FOBTBilling.AsNoTracking().FirstOrDefaultAsync(x => x.FOBTId == request.FobtId && x.BillingProductCode == request.BillingProductCode, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error retrieving billing data with FOBT ID:{request.FobtId}", request.FobtId);
            }

            return null;
        }
    }
}
