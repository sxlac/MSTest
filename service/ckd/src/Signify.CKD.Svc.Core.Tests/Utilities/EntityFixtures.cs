using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.ApiClient.Response;
using Signify.CKD.Svc.Core.Data.Entities;

namespace Signify.CKD.Svc.Core.Tests.Utilities
{
    public class EntityFixtures
    {
        public ProviderRs MockProviderRs()
        {
            return new ProviderRs();
        }

        public CKDStatus MockCKDStatus()
        {
            return new CKDStatus
            {
                CKDStatusCodeId = CKDStatusCode.CKDPerformed.CKDStatusCodeId
            };
        }

        public CKDPerformed MockCKDPerformed()
        {
            return new CKDPerformed();
        }
    }
}