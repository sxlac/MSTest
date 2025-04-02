using System;
using NServiceBus;

namespace Signify.CKD.Svc.Core.Sagas.Models
{
	public class UpdateInventorySagaData : ContainSagaData
	{
		public Guid CorrelationId { get; set; }
		public int CKDId { get; set; }

	}
}
