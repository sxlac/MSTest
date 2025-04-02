using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data;
using System;

namespace Signify.PAD.Svc.Core.Queries
{
    public class GetPadByMemberPlanId : IRequest<Data.Entities.PAD>
    {
        public int MemberPlanId { get; }

        public DateTime DateOfService { get; }

        /// <summary>
        /// When looking up a PAD evaluation by MemberPlanId, DateOfService is required to be included, because there
        /// can be multiple evaluations with the same MemberPlanId across multiple days 
        /// </summary>
        /// <param name="memberPlanId"></param>
        /// <param name="dateOfService"></param>
        public GetPadByMemberPlanId(int memberPlanId, DateTime dateOfService)
        {
            MemberPlanId = memberPlanId;
            DateOfService = dateOfService;
        }
    }

    /// <summary>
    /// Get PAD details from database by MemberPlanId.
    /// </summary>
    public class GetPadByMemberPlanIdHandler : IRequestHandler<GetPadByMemberPlanId, Data.Entities.PAD>
    {
        private readonly PADDataContext _dataContext;

        public GetPadByMemberPlanIdHandler(PADDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [Trace]
        public Task<Data.Entities.PAD> Handle(GetPadByMemberPlanId request, CancellationToken cancellationToken)
        {
            return _dataContext.PAD
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.MemberPlanId == request.MemberPlanId &&
                    p.DateOfService.HasValue &&
                    p.DateOfService.Value.Date == request.DateOfService.Date, // Grab the date portion itself, just in case a time is included
                    cancellationToken);
        }
    }
}
