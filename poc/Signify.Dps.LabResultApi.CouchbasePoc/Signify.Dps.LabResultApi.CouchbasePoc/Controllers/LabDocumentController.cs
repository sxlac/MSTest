using Couchbase.Core.Exceptions.KeyValue;
using Microsoft.AspNetCore.Mvc;
using Signify.Dps.LabResultApi.CouchbasePoc.Configs;
using Signify.Dps.LabResultApi.CouchbasePoc.Data;
using Signify.Dps.LabResultApi.CouchbasePoc.Models;
using System.Net.Mime;

namespace Signify.Dps.LabResultApi.CouchbasePoc.Controllers;

[ApiController]
[Route("[controller]")]
public class LabDocumentController : ControllerBase
{
    private readonly ILogger<LabDocumentController> _logger;
    private readonly ILabDocumentRepository _repository;
    private readonly IDocumentControllerConfig _controllerConfig;

    public LabDocumentController(ILogger<LabDocumentController> logger,
        ILabDocumentRepository repository,
        IDocumentControllerConfig controllerConfig)
    {
        _logger = logger;
        _repository = repository;
        _controllerConfig = controllerConfig;
    }

    /// <summary>
    /// Saves a document to the database
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> SaveDocument([FromBody] CreateLabDocumentRequest request, /*IFormFile file,*/ CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request with OrderId={OrderId}", request.OrderId);

        var allBytes = new List<byte>();

        // for simplicity for this POC, instead of the request containing the document itself,
        // just pull it from the file system based on the config; could alternatively use
        // IFormFile or another stream of data
        await using (var stream = System.IO.File.OpenRead(_controllerConfig.InputDocumentFilePath))
        {
            const int bufferCapacity = 256;

            var buffer = new Memory<byte>(new byte[bufferCapacity]);

            int countBytesRead;
            do
            {
                countBytesRead = await stream.ReadAsync(buffer, cancellationToken);

                if (!buffer.IsEmpty)
                {
                    // not ideal, but this may really be our best option
                    // would need to consider resource usage and limits
                    allBytes.AddRange(buffer.Span);
                }
            } while (countBytesRead == bufferCapacity); // likely not at the end of the stream
        }

        if (!allBytes.Any())
            return base.BadRequest("File is empty");

        var byteArray = allBytes.ToArray();

        var documentId = await _repository.SaveDocument(byteArray, cancellationToken);

        return CreatedAtAction(nameof(GetDocument), new { documentId }, byteArray);
    }

    /// <summary>
    /// Attempts to retrieve a document from the database
    /// </summary>
    /// <param name="documentId">Identifier of the document, as returned from the POST endpoint</param>
    /// <param name="savePath">Optional, path to write the file to on the file system,
    /// to verify roundtrip that the byte array saved can be written back and still
    /// be opened properly</param>
    /// <param name="cancellationToken" />
    [HttpGet]
    public async Task<ActionResult> GetDocument([FromQuery] string documentId, [FromQuery] string savePath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var buffer = await _repository.GetDocument(documentId, cancellationToken);

            if (buffer.Memory.IsEmpty)
                return NotFound();

            if (!string.IsNullOrEmpty(savePath))
            {
                await using var stream = System.IO.File.OpenWrite(savePath);

                await stream.WriteAsync(buffer.Memory, cancellationToken);
            }

            return base.File(buffer.Memory.ToArray(), MediaTypeNames.Application.Pdf); // for this POC, only testing with pdfs
        }
        catch (DocumentNotFoundException)
        {
            return NotFound();
        }
    }
}
