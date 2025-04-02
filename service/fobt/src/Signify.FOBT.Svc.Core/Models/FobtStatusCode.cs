using Signify.FOBT.Svc.Core.Constants;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public abstract class FobtStatusCode
{
    public static string ProductCode => ApplicationConstants.PRODUCT_CODE;
    public int? EvaluationId { get; set; }
    public long? MemberPlanId { get; set; }
    public int? ProviderId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTimeOffset ReceivedDateTime { get; set; }
}