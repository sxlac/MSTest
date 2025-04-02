using Signify.FOBT.Svc.Core.Data.Entities;
using System;

namespace Signify.FOBT.Svc.Core.Tests.Mocks.Models;

public static class FobtBarcodeHistoryMock
{
    public static FOBTBarcodeHistory BuildFobtBarcodeHistory()
    {
        return new FOBTBarcodeHistory
        { 
            FOBTBarcodeHistoryId = 1,
            FOBTId = 1,
            Barcode = "01234567890",
            OrderCorrelationId = new Guid("abcd0123-9876-4562-abcd-0123456789ab"),
            CreatedDateTime = DateTime.UtcNow.AddDays(-1)
        };
    }

    public static FOBTBarcodeHistory BuildFobtBarcodeHistory(Core.Data.Entities.FOBT fobt)
    {
        return new FOBTBarcodeHistory
        {
            FOBTBarcodeHistoryId = 1,
            FOBTId = fobt.FOBTId,
            FOBT = fobt,
            Barcode = "01234567890",
            OrderCorrelationId = new Guid("abcd0123-9876-4562-abcd-0123456789ab"),
            CreatedDateTime = DateTime.UtcNow.AddDays(-1)
        };
    }
}