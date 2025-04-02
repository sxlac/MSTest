using System;
using NServiceBus;

namespace Signify.A1C.Svc.Core.Sagas.Models
{
    public class UpdateInventorySagaData : ContainSagaData
    {
        public Guid CorrelationId { get; set; }
        public int HBA1CId { get; set; }
    }
}