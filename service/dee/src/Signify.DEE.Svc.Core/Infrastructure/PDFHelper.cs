using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Signify.DEE.Svc.Core.Infrastructure;

public class PdfHelper<T>
{
    public PdfHelper(IConfiguration configuration)
    {
        IronPdf.License.LicenseKey = configuration["IronPDF:Key"];
    }
    public  bool IsValidPdf(byte[] pdf, ILogger<T>  _log)
    {
        var result = true;
        try
        {
            var pdfDoc = new IronPdf.PdfDocument(pdf);
            pdfDoc.ExtractAllText();
        }
        catch (Exception ex)
        {
            _log.LogError(ex.Message);
            result = false;
        }
        return result;
    }
}