using Couchbase.Core.IO.Transcoders;
using Couchbase.Core.Retry;
using Couchbase.KeyValue;
using Signify.Dps.LabResultApi.CouchbasePoc.Services;
using System.Buffers;

namespace Signify.Dps.LabResultApi.CouchbasePoc.Data;

public interface ILabDocumentRepository
{
    /// <summary>
    /// Saves the contents of a document to the database
    /// </summary>
    /// <returns>Identifier of the new document</returns>
    Task<string> SaveDocument(byte[] document, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a document from the database
    /// </summary>
    Task<IMemoryOwner<byte>> GetDocument(string documentId, CancellationToken cancellationToken);
}

public class LabDocumentRepository : CouchbaseRepository, ILabDocumentRepository
{
    protected override string CollectionName => CouchbaseConstants.LabPdfCollection;

    public LabDocumentRepository(ICouchbaseCollectionProvider collectionProvider)
        : base(collectionProvider)
    {
    }

    /// <inheritdoc />
    public async Task<string> SaveDocument(byte[] document, CancellationToken cancellationToken)
    {
        var documentId = GenerateDocumentId();

        await Execute(async collection => await collection.InsertAsync(documentId, document, CreateInsertOptions(cancellationToken)));

        return documentId;
    }

    /// <inheritdoc />
    public async Task<IMemoryOwner<byte>> GetDocument(string documentId, CancellationToken cancellationToken)
    {
        return await await Execute(async collection =>
        {
            using var result = await collection.GetAsync(documentId, CreateGetOptions(cancellationToken));

            return result.ContentAs<IMemoryOwner<byte>>();
        });
    }

    private static InsertOptions CreateInsertOptions(CancellationToken cancellationToken)
    {
        // these are a few of the options supported
        return new InsertOptions()
            .CancellationToken(cancellationToken)
            .RetryStrategy(new BestEffortRetryStrategy())
            // we're storing these documents as raw binary, so do not want it to be serialized or required to be json
            .Transcoder(new RawBinaryTranscoder());
    }

    private static GetOptions CreateGetOptions(CancellationToken cancellationToken)
    {
        // these are a few of the options supported
        return new GetOptions()
            .CancellationToken(cancellationToken)
            .Transcoder(new RawBinaryTranscoder());
    }
}
