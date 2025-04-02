using Signify.FOBT.Svc.Core.Data.Entities;
using System;

namespace Signify.FOBT.Svc.Core.Tests.Mocks.Models;

public static class PdfToClientMock
{
    public static PDFToClient BuildPdfToClient()
    {
        return new PDFToClient
        {
            PDFDeliverId = 1234,
            EvaluationId = 123456,
            DeliveryDateTime = DateTime.UtcNow.AddHours(-1),
            DeliveryCreatedDateTime = DateTime.UtcNow.AddHours(-2),
            BatchId = 1,
            FOBTId = 1234,
            CreatedDateTime = DateTime.UtcNow.AddMinutes(-1)
        };
    }
}