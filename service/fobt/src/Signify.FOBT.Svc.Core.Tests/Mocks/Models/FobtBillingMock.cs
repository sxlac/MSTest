using Signify.FOBT.Svc.Core.Data.Entities;
using System;

namespace Signify.FOBT.Svc.Core.Tests.Mocks.Models;

public static class FobtBillingMock
{
    public static FOBTBilling BuildFobtBilling()
    {
        return new FOBTBilling
        { 
            Id = 1,
            BillId = Guid.NewGuid().ToString(),
            BillingProductCode = "FOBT-Left",
            FOBTId = 1,
            CreatedDateTime = DateTime.UtcNow
        };
    }
}