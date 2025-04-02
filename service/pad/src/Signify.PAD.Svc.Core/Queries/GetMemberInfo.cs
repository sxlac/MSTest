using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.ApiClient.Response;

namespace Signify.PAD.Svc.Core.Queries
{
    public class GetMemberInfo : IRequest<MemberInfoRs>
    {
        public long MemberPlanId { get; set; }
    }

    public class GetMemberInfoHandler : IRequestHandler<GetMemberInfo, MemberInfoRs>
    {
        private readonly ILogger _logger;
        private readonly IMemberInfoApi _memberInfoApi;

        public GetMemberInfoHandler(ILogger<GetMemberInfoHandler> logger, IMemberInfoApi memberInfoApi)
        {
            _logger = logger;
            _memberInfoApi = memberInfoApi;
        }

        [Trace]
        public async Task<MemberInfoRs> Handle(GetMemberInfo request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Querying Member API");

            var memberInfo = await _memberInfoApi.GetMemberByMemberPlanId(request.MemberPlanId);

            _logger.LogDebug("Received member info from Member API");

            return memberInfo;
        }
    }
}
