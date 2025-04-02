using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Events;

/// <summary>
/// Event received over Kafka from the Signify CDI service
///
/// See https://dev.azure.com/signifyhealth/HCC/_git/cdi?path=/docs/apis/asyncapi.yaml
/// </summary>
#pragma warning disable S101 // SonarQube - Types should be named in PascalCase
// ReSharper disable once InconsistentNaming
[ExcludeFromCodeCoverage]
public class CDIEvaluationHoldReleasedEvent
#pragma warning restore S101
{
    /// <summary>
    /// Identifier of this event
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Identifier of this hold
    /// </summary>
    public Guid HoldId { get; set; }

    /// <summary>
    /// Identifier of this evaluation
    /// </summary>
    public int EvaluationId { get; set; }

    /// <summary>
    /// List of products that were included in this evaluation
    /// </summary>
    public ICollection<ProductHold> Products { get; set; } = new List<ProductHold>();

    /// <summary>
    /// Date and time the hold release was created
    /// </summary>
    public DateTimeOffset ReleasedOn { get; set; }

    /// <summary>
    /// Date and time the hold release was sent
    /// </summary>
    public DateTimeOffset SentAt { get; set; }
}