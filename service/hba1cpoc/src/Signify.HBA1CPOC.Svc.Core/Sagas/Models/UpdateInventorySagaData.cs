using System;
using System.Diagnostics.CodeAnalysis;
using NServiceBus;

namespace Signify.HBA1CPOC.Svc.Core.Sagas.Models;

[ExcludeFromCodeCoverage]
public class UpdateInventorySagaData : ContainSagaData
{
	public Guid CorrelationId { get; set; }
	public int HBA1CPOCId { get; set; }

}