using NServiceBus;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Events;

/// <summary>
/// Event which originates from the Labs API, signalling that a new kit (which therefore has a different barcode)
/// has been sent to the member.
/// </summary>
/// <remarks>
/// There are a handful of reasons this can happen, but the most common reasons are:
///
/// 1) Kit shelf life expired, so the sample cannot be tested;
/// 2) The sample expired;
/// 3) The sample has been on hold (unable to confidently know which order the sample belongs to) for more than 30d
///
/// When a sample cannot be tested for one of these, or other, various reasons, a new kit is typically sent to
/// the member.
/// </remarks>
[ExcludeFromCodeCoverage]
public class BarcodeUpdate : IMessage
{
    public int? MemberPlanId { get; set; }
    public int? EvaluationId { get; set; }
    public string ProductCode { get; set; }
    /// <summary>
    /// Barcode of the kit
    /// </summary>
    public string Barcode { get; set; }
    public Guid? OrderCorrelationId { get; set; }
}