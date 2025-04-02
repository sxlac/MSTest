using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Events;

namespace Signify.PAD.Svc.Core.Tests.Utilities;

public class EntityFixtures
{
    public ProviderRs MockProviderRs()
    {
        return new ProviderRs();
    }
    public PADStatus MockPADStatus()
    {
        return new PADStatus
        {
            PADStatusCodeId = PADStatusCode.PadPerformed.PADStatusCodeId
        };
    }
    public PADPerformed MockPADPerformed()
    {
        return new PADPerformed();
    }
}