using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;

namespace Signify.HBA1CPOC.Svc.Core.Queries
{
    public class GetProviderInfo : IRequest<ProviderRs>
    {
        public int ProviderId { get; set; }
    }

    public class GetProviderInfoHandler : IRequestHandler<GetProviderInfo, ProviderRs>
    {
        private readonly IProviderApi _providerApi;
        public GetProviderInfoHandler(IProviderApi providerApi) => _providerApi = providerApi;

        [Trace]
        public async Task<ProviderRs> Handle(GetProviderInfo request, CancellationToken cancellationToken)
        {
            if (request.ProviderId == default)
            {
                return null;
            }

            var providerResponse = await _providerApi.GetProviderById(request.ProviderId);

            if (!providerResponse.IsSuccessStatusCode || providerResponse.Content == null)
            {
                //Failed to get provider details.Throw exception to hit retry process
                throw new ApplicationException($"Unable to get provider details - Error:", providerResponse.Error);
            }
            return providerResponse.Content;
        }
    }
}
