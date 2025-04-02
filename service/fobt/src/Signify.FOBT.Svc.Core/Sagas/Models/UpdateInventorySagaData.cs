using NServiceBus;
using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.FOBT.Svc.Core.Sagas.Models;

[ExcludeFromCodeCoverage]
public class UpdateInventorySagaData : ContainSagaData
{
    public Guid CorrelationId { get; set; }
    public int FOBTId { get; set; }
}