using MediatR;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.ApiClients.ProviderApi;
using Signify.uACR.Core.ApiClients.ProviderApi.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.uACR.Core.Queries;

public class QueryProviderInfo(int providerId) : IRequest<ProviderInfo>
{
    public int ProviderId { get; } = providerId;
}

public class QueryProviderInfoHandler(ILogger<QueryProviderInfoHandler> logger, IProviderApi providerApi)
    : IRequestHandler<QueryProviderInfo, ProviderInfo>
{
    public async Task<ProviderInfo> Handle(QueryProviderInfo request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying Provider API for ProviderId={ProviderId}", request.ProviderId);

        var providerInfo = await providerApi.GetProviderById(request.ProviderId).ConfigureAwait(false);

        logger.LogDebug("Received provider information for ProviderId={ProviderId}", request.ProviderId);

        return providerInfo;
    }
}