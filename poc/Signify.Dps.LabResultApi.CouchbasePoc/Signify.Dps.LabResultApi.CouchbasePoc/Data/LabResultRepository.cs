using Couchbase.KeyValue;
using Signify.Dps.LabResultApi.CouchbasePoc.Services;

namespace Signify.Dps.LabResultApi.CouchbasePoc.Data;

public interface ILabResultRepository
{
    /// <summary>
    /// Saves the lab result to the database
    /// </summary>
    /// <returns>Identifier of the new document</returns>
    Task<string> SaveLabResult(string documentJson, CancellationToken cancellationToken);

    Task<string> GetLabResult(string documentId, CancellationToken cancellationToken);
}

public class LabResultRepository : CouchbaseRepository, ILabResultRepository
{
    protected override string CollectionName => CouchbaseConstants.LabResultCollection;

    public LabResultRepository(ICouchbaseCollectionProvider collectionProvider)
        : base(collectionProvider)
    {
    }

    public Task<string> SaveLabResult(string documentJson, CancellationToken cancellationToken)
        => InsertAsync(documentJson, cancellationToken);

    private async Task<string> InsertAsync<T>(T document, CancellationToken cancellationToken)
    {
        var documentId = GenerateDocumentId();

        await Execute(async collection =>
        {
            await collection.InsertAsync(documentId, document, CreateInsertOptions(cancellationToken));
        });

        return documentId;
    }

    public async Task<string> GetLabResult(string documentId, CancellationToken cancellationToken)
    {
        return await await Execute(async collection =>
        {
            using var result = await collection.GetAsync(documentId, CreateGetOptions(cancellationToken));

            return result.ContentAs<string>();
        });
    }

    private static InsertOptions CreateInsertOptions(CancellationToken cancellationToken)
    {
        // these are a few of the options supported
        return new InsertOptions()
            .CancellationToken(cancellationToken);
    }

    private static GetOptions CreateGetOptions(CancellationToken cancellationToken)
    {
        // these are a few of the options supported
        return new GetOptions()
            .CancellationToken(cancellationToken);
    }
}
