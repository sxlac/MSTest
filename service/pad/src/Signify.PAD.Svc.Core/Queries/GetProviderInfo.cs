using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.ApiClient.Response;

namespace Signify.PAD.Svc.Core.Queries
{
    public class GetProviderInfo : IRequest<ProviderRs>
    {
        public int ProviderId { get; set; }
    }

    public class GetProviderInfoHandler : IRequestHandler<GetProviderInfo, ProviderRs>
    {
        private readonly ILogger _logger;
        private readonly IProviderApi _providerApi;

        public GetProviderInfoHandler(ILogger<GetProviderInfoHandler> logger,
            IProviderApi providerApi)
        {
            _logger = logger;
            _providerApi = providerApi;
        }

        [Trace]
        public async Task<ProviderRs> Handle(GetProviderInfo request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Querying Provider API for ProviderId={ProviderId}", request.ProviderId);

            var providerInfo = await _providerApi.GetProviderById(request.ProviderId);

            _logger.LogDebug("Received provider information for ProviderId={ProviderId}", request.ProviderId);

            return providerInfo;
        }
    }
}
