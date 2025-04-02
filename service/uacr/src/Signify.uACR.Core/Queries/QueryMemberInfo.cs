using MediatR;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.ApiClients.MemberApi;
using Signify.uACR.Core.ApiClients.MemberApi.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.uACR.Core.Queries;

public class QueryMemberInfo(long memberPlanId) : IRequest<MemberInfo>
{
    public long MemberPlanId { get; } = memberPlanId;
}

public class QueryMemberInfoHandler(ILogger<QueryMemberInfoHandler> logger, IMemberApi memberApi)
    : IRequestHandler<QueryMemberInfo, MemberInfo>
{
    public async Task<MemberInfo> Handle(QueryMemberInfo request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying Member API for member by MemberPlanId={MemberPlanId}", request.MemberPlanId);

        var memberInfo = await memberApi.GetMemberByMemberPlanId(request.MemberPlanId).ConfigureAwait(false);

        logger.LogDebug("Received member info for MemberPlanId={MemberPlanId} with CenseoId={CenseoId}",
            request.MemberPlanId, memberInfo.CenseoId);

        return memberInfo;
    }
}