using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.ApiClients.MemberApi;
using Signify.Spirometry.Core.ApiClients.MemberApi.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries
{
    public class QueryMemberInfo : IRequest<MemberInfo>
    {
        public long MemberPlanId { get; }

        public QueryMemberInfo(long memberPlanId)
        {
            MemberPlanId = memberPlanId;
        }
    }

    public class QueryMemberInfoHandler : IRequestHandler<QueryMemberInfo, MemberInfo>
    {
        private readonly ILogger _logger;
        private readonly IMemberApi _memberApi;

        public QueryMemberInfoHandler(ILogger<QueryMemberInfoHandler> logger, IMemberApi memberApi)
        {
            _logger = logger;
            _memberApi = memberApi;
        }

        public async Task<MemberInfo> Handle(QueryMemberInfo request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Querying Member API for member by MemberPlanId={MemberPlanId}", request.MemberPlanId);

            var memberInfo = await _memberApi.GetMemberByMemberPlanId(request.MemberPlanId).ConfigureAwait(false);

            _logger.LogDebug("Received member info for MemberPlanId={MemberPlanId} with CenseoId={CenseoId}",
                request.MemberPlanId, memberInfo.CenseoId);

            return memberInfo;
        }
    }
}
