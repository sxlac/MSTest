using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public partial class ExamImage
{
    public int ExamImageId { get; set; }
    public int ExamId { get; set; }
    public string ImageLocalId { get; set; }
    public string ImageQuality { get; set; }
    public string ImageType { get; set; }
    public int? LateralityCodeId { get; set; }
    [Obsolete("Do not use in new code, will be removed in ANC-3730")]
    public bool? Gradable { get; set; }
    [Obsolete("Do not use in new code, will be removed in ANC-3730")]
    public string NotGradableReasons { get; set; }
    public virtual Exam Exam { get; set; }
    public virtual LateralityCode LateralityCode { get; set; }
}