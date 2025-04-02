using System;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class ExamImageModel
{
    public int ImageId { get; set; }
    public int ExamId { get; set; }
    public string ImageLocalId { get; set; }
    public string Laterality { get; set; }
    public string ImageQuality { get; set; }
    public string ImageType { get; set; }
    [Obsolete("Do not use in new code, will be removed in ANC-3730")]
    public bool Gradable { get; set; }
    [Obsolete("Do not use in new code, will be removed in ANC-3730")]
    public List<string> NotGradableReasons { get; set; } = new List<string>();

    protected bool Equals(ExamImageModel other)
    {
        return ImageId == other.ImageId && ExamId == other.ExamId && string.Equals(Laterality, other.Laterality) && string.Equals(ImageQuality, other.ImageQuality) && string.Equals(ImageType, other.ImageType) && string.Equals(Gradable, other.Gradable) && string.Equals(NotGradableReasons, other.NotGradableReasons);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ExamImageModel)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = ImageId;
            hashCode = (hashCode * 397) ^ ExamId;
            hashCode = (hashCode * 397) ^ (Laterality != null ? Laterality.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ImageQuality != null ? ImageQuality.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ImageType != null ? ImageType.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (NotGradableReasons != null ? NotGradableReasons.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ Gradable.GetHashCode();
            hashCode = (hashCode * 397) ^ ImageId.GetHashCode();
            return hashCode;
        }
    }
}