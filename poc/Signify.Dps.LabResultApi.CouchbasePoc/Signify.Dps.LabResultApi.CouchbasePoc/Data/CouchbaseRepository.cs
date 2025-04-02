using Couchbase.KeyValue;
using Signify.Dps.LabResultApi.CouchbasePoc.Services;

namespace Signify.Dps.LabResultApi.CouchbasePoc.Data;

public abstract class CouchbaseRepository
{
    private readonly ICouchbaseCollectionProvider _collectionProvider;

    /// <summary>
    /// Name of the Couchbase collection for this repository
    /// </summary>
    protected abstract string CollectionName { get; }

    protected CouchbaseRepository(ICouchbaseCollectionProvider collectionProvider)
    {
        _collectionProvider = collectionProvider;
    }

    /// <summary>
    /// Generates a new document id
    /// </summary>
    protected virtual string GenerateDocumentId()
        => Guid.NewGuid().ToString();

    /// <summary>
    /// Executes an action against the Couchbase collection
    /// </summary>
    protected async Task<T> Execute<T>(Func<ICouchbaseCollection, T> action)
    {
        var collection = await _collectionProvider.GetCollection(CollectionName);

        var result = action(collection);

        return result;
    }
}
