using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.ApiClient.Response;

namespace Signify.FOBT.Svc.Core.Queries
{
    public class GetProviderInfo : IRequest<ProviderInfoRs>
    {
        public int ProviderId { get; set; }
    }

    public class GetProviderInfoHandler : IRequestHandler<GetProviderInfo, ProviderInfoRs>
    {
        private readonly IProviderApi _providerApi;
        private readonly ILogger<GetProviderInfoHandler> _logger;
        private readonly IMapper _mapper;

        public GetProviderInfoHandler(IProviderApi providerApi, ILogger<GetProviderInfoHandler> logger, IMapper mapper)
        {
            _providerApi = providerApi;
            _logger = logger;
            _mapper = mapper;
        }
        
        [Trace]
        public async Task<ProviderInfoRs> Handle(GetProviderInfo request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Start Handle GetProviderInfo");

            if (request == null || request.ProviderId == default)
            {
                _logger.LogDebug("Request or ProviderId is null");
                throw new ApplicationException("Request or ProviderId is null");
            }

            var providerResponse = await _providerApi.GetProviderById(request.ProviderId);
            if (!providerResponse.IsSuccessStatusCode || providerResponse.Content == null)
            {
                //Failed to get provider details.Throw exception to hit retry process
                throw new ApplicationException("Unable to get provider details - Error:", providerResponse.Error);
            }
            var providerInfo = _mapper.Map<ProviderInfoRs>(providerResponse.Content);

            _logger.LogDebug("End Handle GetProviderInfo");
            return providerInfo;
        }
    }
}
