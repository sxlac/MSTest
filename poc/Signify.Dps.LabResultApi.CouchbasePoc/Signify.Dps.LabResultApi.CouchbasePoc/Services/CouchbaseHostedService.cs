using Couchbase.Extensions.DependencyInjection;

namespace Signify.Dps.LabResultApi.CouchbasePoc.Services;

/// <remarks>
/// The Couchbase documentation recommends manually closing connections on shutdown
///
/// https://docs.couchbase.com/dotnet-sdk/current/howtos/managing-connections.html#shutdown
/// </remarks>
public class CouchbaseHostedService : IHostedService
{
    private readonly ICouchbaseLifetimeService _lifetimeService;

    public CouchbaseHostedService(ICouchbaseLifetimeService lifetimeService)
    {
        _lifetimeService = lifetimeService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _lifetimeService.CloseAsync();
    }
}
