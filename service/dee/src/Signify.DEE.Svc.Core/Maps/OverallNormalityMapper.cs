using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.Maps;

public class OverallNormalityMapper(ILogger<OverallNormalityMapper> logger) : IOverallNormalityMapper
{
    public string GetOverallNormality(IEnumerable<string> normalityIndicators)
    {
        // See the Eye Normality section in https://wiki.signifyhealth.com/display/AncillarySvcs/DEE+Business+Rules

        // Normality for an eye is determined by looking at all findings for that given eye. The eye's normality is determined by the worst finding, therefore with the following order of precedence:
        // 1) Abnormal
        // 2) Undetermined
        // 3) Normal

        bool hasUndetermined = false, hasNormal = false;

        foreach (var normality in normalityIndicators)
        {
            switch (normality)
            {
                case Constants.ApplicationConstants.NormalityIndicator.Abnormal:
                    return normality; // Return right away; having any as Abnormal means the whole side is Abnormal
                case Constants.ApplicationConstants.NormalityIndicator.Undetermined:
                    hasUndetermined = true;
                    break;
                case Constants.ApplicationConstants.NormalityIndicator.Normal:
                    hasNormal = true;
                    break;
                default: // Should not happen; edge case
                    logger.LogWarning("Found an invalid Normality Indicator value, treating as Undetermined: {Normality}", normality);
                    hasUndetermined = true;
                    break;
            }
        }

        if (hasUndetermined || !hasNormal)
            return Constants.ApplicationConstants.NormalityIndicator.Undetermined; // Undetermined takes precedence

        return Constants.ApplicationConstants.NormalityIndicator.Normal;
    }
}