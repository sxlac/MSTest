using Signify.FOBT.Svc.Core.Data.Entities;
using System;

namespace Signify.FOBT.Svc.Core.Tests.Mocks.Models;

public static class ProviderPayMock
{
    public static ProviderPay BuildProviderPay(Core.Data.Entities.FOBT fobt)
    {
        return new ProviderPay
        {
            Id = 1,
            PaymentId = "123456ABC",
            CreatedDateTime = DateTimeOffset.UtcNow,
            FOBTId = fobt.FOBTId
        };
    }
}