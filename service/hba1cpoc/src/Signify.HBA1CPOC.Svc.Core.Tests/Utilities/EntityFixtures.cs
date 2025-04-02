using Signify.HBA1CPOC.Sagas;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using System;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Utilities;

public static class EntityFixtures
{
    public static HBA1CPOCStatus MockCreateHBA1CPOCStatus()
    {
        return new HBA1CPOCStatus(1, HBA1CPOCStatusCode.HBA1CPOCPerformed, new Core.Data.Entities.HBA1CPOC(), DateTimeOffset.UtcNow);
    }

    public static InventoryUpdateReceived MockInventoryUpdateReceived()
    {
        return new InventoryUpdateReceived(Guid.Empty, default, default, default, default, default, default);
    }
}