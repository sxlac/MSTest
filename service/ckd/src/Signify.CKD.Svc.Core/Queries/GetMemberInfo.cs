using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.CKD.Svc.Core.ApiClient;
using Signify.CKD.Svc.Core.ApiClient.Response;

namespace Signify.CKD.Svc.Core.Queries
{
    public class GetMemberInfo : IRequest<MemberInfoRs>
    {
        public int MemberPlanId { get; set; }
    }

    public class GetMemberInfoHandler : IRequestHandler<GetMemberInfo, MemberInfoRs>
    {
        private readonly IMemberInfoApi _memberInfoApi;
        private readonly IMapper _mapper;
        private readonly ILogger<GetMemberInfoHandler> _logger;

        public GetMemberInfoHandler(ILogger<GetMemberInfoHandler> logger, IMemberInfoApi memberInfoApi, IMapper mapper)
        {
            _logger = logger;
            _memberInfoApi = memberInfoApi;
            _mapper = mapper;
        }

        [Trace]
        public async Task<MemberInfoRs> Handle(GetMemberInfo request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Start Handle GetMemberInfo");

            //To ask what to do in this case
            if (request == null || request.MemberPlanId == default)
            {
                throw new ApplicationException($"Request or MemberPlanId is null");
            }

            var memberResponse = await _memberInfoApi.GetMemberInfoById(request.MemberPlanId);
            if (!memberResponse.IsSuccessStatusCode || memberResponse.Content == null)
            {
                //Failed to get Member details.Throw exception to hit retry process
                throw new ApplicationException(
                    "Unable to get Member details.");
            }

            var memberInfoRs = _mapper.Map<MemberInfoRs>(memberResponse.Content);

            _logger.LogDebug("End Handle GetMemberInfo");

            return memberInfoRs;
        }
    }
}
