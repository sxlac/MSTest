using System;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Signify.DEE.Svc.Core.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Signify.DEE.Svc.Core.Messages.Queries;

/// <summary>
/// Query to get Pdf Data from string.
/// </summary>
public class GetPdfDataFromString : IRequest<byte[]>
{
    public string PdfData { get; set; }
    public long EvaluationId { get; set; }
}

public class GetPdfDataFromStringHandler(ILogger<GetPdfDataFromStringHandler> logger, IConfiguration config)
    : IRequestHandler<GetPdfDataFromString, byte[]>
{
    public async Task<byte[]> Handle(GetPdfDataFromString request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PdfData)) return null;

        try
        {
            //try to convert to bytearray
            var pdfDataBytes = Convert.FromBase64String(request.PdfData);

            //Use IronPdf to check validity.
            var pdfHelp = new PdfHelper<GetPdfDataFromStringHandler>(config);
            var validPdf = await Task.Run(() => pdfHelp.IsValidPdf(pdfDataBytes, logger), cancellationToken);
            return !validPdf ? null : pdfDataBytes;
        }
        catch (FormatException)
        {
            logger.LogInformation("PDF input string was not in correct format for EvaluationId={EvaluationId}", request.EvaluationId);
            return null;
        }
    }
}