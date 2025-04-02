using Couchbase.Core.Exceptions.KeyValue;
using Microsoft.AspNetCore.Mvc;
using Signify.Dps.LabResultApi.CouchbasePoc.Data;
using Signify.Dps.LabResultApi.CouchbasePoc.Models;
using System.Text.Json;

namespace Signify.Dps.LabResultApi.CouchbasePoc.Controllers;

[ApiController]
[Route("[controller]")]
public class LabResultController : ControllerBase
{
    private readonly ILabResultRepository _repository;

    public LabResultController(ILabResultRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Saves a lab result record to the database
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> SaveLabResult([FromBody] CreateLabResultRequest request, CancellationToken cancellationToken)
    {
        var element = JsonSerializer.SerializeToElement(request);

        var rawJson = element.GetRawText();

        var documentId = await _repository.SaveLabResult(rawJson, cancellationToken);

        return CreatedAtAction(nameof(GetLabResult), new { documentId }, rawJson);
    }

    /// <summary>
    /// Attempts to retrieve a lab result record from the database
    /// </summary>
    /// <param name="documentId">Identifier of the document, as returned from the POST endpoint</param>
    /// <param name="cancellationToken" />
    [HttpGet]
    public async Task<ActionResult> GetLabResult([FromQuery] string documentId, CancellationToken cancellationToken)
    {
        try
        {
            var document = await _repository.GetLabResult(documentId, cancellationToken);

            return document != null
                ? Ok(document)
                : NotFound();
        }
        catch (DocumentNotFoundException)
        {
            return NotFound();
        }
    }
}
