using MediatR;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.ApiClients.MemberApi;
using Signify.eGFR.Core.ApiClients.MemberApi.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.eGFR.Core.Queries;

public class QueryMemberInfo(long memberPlanId) : IRequest<MemberInfo>
{
    public long MemberPlanId { get; } = memberPlanId;
}

public class QueryMemberInfoHandler(ILogger<QueryMemberInfoHandler> logger, IMemberApi memberApi)
    : IRequestHandler<QueryMemberInfo, MemberInfo>
{
    private readonly ILogger _logger = logger;

    public async Task<MemberInfo> Handle(QueryMemberInfo request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Querying Member API for member by MemberPlanId={MemberPlanId}", request.MemberPlanId);

        var memberInfo = await memberApi.GetMemberByMemberPlanId(request.MemberPlanId).ConfigureAwait(false);

        _logger.LogDebug("Received member info for MemberPlanId={MemberPlanId} with CenseoId={CenseoId}",
            request.MemberPlanId, memberInfo.CenseoId);

        return memberInfo;
    }
}