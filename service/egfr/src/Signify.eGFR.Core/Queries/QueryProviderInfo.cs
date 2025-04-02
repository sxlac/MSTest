using MediatR;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.ApiClients.ProviderApi;
using Signify.eGFR.Core.ApiClients.ProviderApi.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.eGFR.Core.Queries;

public class QueryProviderInfo(int providerId) : IRequest<ProviderInfo>
{
    public int ProviderId { get; } = providerId;
}

public class QueryProviderInfoHandler(ILogger<QueryProviderInfoHandler> logger, IProviderApi providerApi)
    : IRequestHandler<QueryProviderInfo, ProviderInfo>
{
    private readonly ILogger _logger = logger;

    public async Task<ProviderInfo> Handle(QueryProviderInfo request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Querying Provider API for ProviderId={ProviderId}", request.ProviderId);

        var providerInfo = await providerApi.GetProviderById(request.ProviderId).ConfigureAwait(false);

        _logger.LogDebug("Received provider information for ProviderId={ProviderId}", request.ProviderId);

        return providerInfo;
    }
}