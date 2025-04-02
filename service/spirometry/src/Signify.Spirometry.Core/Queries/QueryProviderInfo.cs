using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.ApiClients.ProviderApi;
using Signify.Spirometry.Core.ApiClients.ProviderApi.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries
{
    public class QueryProviderInfo : IRequest<ProviderInfo>
    {
        public int ProviderId { get; }

        public QueryProviderInfo(int providerId)
        {
            ProviderId = providerId;
        }
    }

    public class QueryProviderInfoHandler : IRequestHandler<QueryProviderInfo, ProviderInfo>
    {
        private readonly ILogger _logger;
        private readonly IProviderApi _providerApi;

        public QueryProviderInfoHandler(ILogger<QueryProviderInfoHandler> logger, IProviderApi providerApi)
        {
            _logger = logger;
            _providerApi = providerApi;
        }

        public async Task<ProviderInfo> Handle(QueryProviderInfo request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Querying Provider API for ProviderId={ProviderId}", request.ProviderId);

            var providerInfo = await _providerApi.GetProviderById(request.ProviderId).ConfigureAwait(false);

            _logger.LogDebug("Received provider information for ProviderId={ProviderId}", request.ProviderId);

            return providerInfo;
        }
    }
}
