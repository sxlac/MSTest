using Couchbase.Extensions.DependencyInjection;
using Couchbase.KeyValue;

namespace Signify.Dps.LabResultApi.CouchbasePoc.Services;

public interface ICouchbaseCollectionProvider
{
    Task<ICouchbaseCollection> GetCollection(string collectionName);
}

public class CouchbaseCollectionProvider : ICouchbaseCollectionProvider
{
    private readonly INamedBucketProvider _bucketProvider;

    public CouchbaseCollectionProvider(INamedBucketProvider bucketProvider)
    {
        _bucketProvider = bucketProvider;
    }

    public async Task<ICouchbaseCollection> GetCollection(string collectionName)
    {
        var bucket = await _bucketProvider.GetBucketAsync();

        var scope = await bucket.ScopeAsync(CouchbaseConstants.Scope);

        return await scope.CollectionAsync(collectionName);
    }
}
