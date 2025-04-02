using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Exceptions;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetMemberInfo : IRequest<MemberInfoRs>
{
    public long EvaluationId { get; set; }
    public Guid EventId { get; set; }
    public long MemberPlanId { get; set; }
}

public class MemberInfoRs
{
    public string FirstName { get; set; }
    public object MiddleName { get; set; }
    public string LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string AddressLineOne { get; set; }
    public string AddressLineTwo { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public string Client { get; set; }
    public string CenseoId { get; set; }
}

public class GetMemberInfoHandler(ILogger<GetMemberInfoHandler> logger, IMemberApi memberInfoApi, IMapper mapper)
    : IRequestHandler<GetMemberInfo, MemberInfoRs>
{
    [Trace]
    public async Task<MemberInfoRs> Handle(GetMemberInfo request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Start Handle GetMemberInfo");

        if (request == null || request.MemberPlanId == default)
        {
            throw new ArgumentException("Request or MemberPlanId is null in GetMemberInfo");
        }

        var memberResponse = await memberInfoApi.GetMember(request.MemberPlanId);
        if (!memberResponse.IsSuccessStatusCode || memberResponse.Content == null)
        {
            throw new ExternalApiException(request.EvaluationId, request.EventId, ApplicationConstants.MemberApi);
        }

        var memberInfoRs = mapper.Map<MemberInfoRs>(memberResponse.Content);
        logger.LogInformation("Obtained CenseoId={CenseoId} for EvaluationId={EvaluationId} and EventId={EventId} from {MemberApi}",
            memberInfoRs.CenseoId, request.EvaluationId, request.EventId, ApplicationConstants.MemberApi);

        return memberInfoRs;
    }
}