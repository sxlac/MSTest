using AutoMapper;
using Signify.PAD.Svc.Core.Models;
using static Signify.PAD.Svc.Core.Constants.Application;

namespace Signify.PAD.Svc.Core.Maps;

public class AoeResultConverter : ITypeConverter<AoeSymptomAnswers, AoeResult>
{
    public AoeResult Convert(AoeSymptomAnswers aoeSymptomAnswers, AoeResult aoeResult, ResolutionContext context)
    {
        aoeResult ??= new AoeResult();
        
        aoeResult.ClinicalSupport =
        [
            new ClinicalSupport { SupportType = ClinicalSupportType.PainInLegs, SupportValue = ((LateralityCodes)aoeSymptomAnswers.LateralityCodeId).ToString() },
            new ClinicalSupport { SupportType = ClinicalSupportType.FootPainDisappearsWalkingOrDangling, SupportValue = aoeSymptomAnswers.FootPainDisappearsWalkingOrDangling.ToString().ToLower() },
            new ClinicalSupport { SupportType = ClinicalSupportType.FootPainDisappearsWithMeds, SupportValue = aoeSymptomAnswers.FootPainDisappearsOtc.ToString().ToLower() },
            new ClinicalSupport { SupportType = ClinicalSupportType.PedalPulseCode, SupportValue = GetPedalPulseCodeDescription((PedalPulseCodes)aoeSymptomAnswers.PedalPulseCodeId) },
            new ClinicalSupport { SupportType = ClinicalSupportType.HasSymptomsForAoeWithRestingLegPain, SupportValue = aoeSymptomAnswers.HasSymptomsForAoeWithRestingLegPain.ToString().ToLower() },
            new ClinicalSupport { SupportType = ClinicalSupportType.HasClinicalSupportForAoeWithRestingLegPain, SupportValue = aoeSymptomAnswers.HasClinicalSupportForAoeWithRestingLegPain.ToString().ToLower() },
            new ClinicalSupport { SupportType = ClinicalSupportType.AoeWithRestingLegPainConfirmed, SupportValue = aoeSymptomAnswers.AoeWithRestingLegPainConfirmed.ToString().ToLower() },
            new ClinicalSupport { SupportType = ClinicalSupportType.ReasonAoeWithRestingLegPainNotConfirmed, SupportValue = aoeSymptomAnswers.ReasonAoeWithRestingLegPainNotConfirmed }
        ];

        return aoeResult;
    }

    private static string GetPedalPulseCodeDescription(PedalPulseCodes pedalPulseCode)
    {
        switch (pedalPulseCode)
        {
            case PedalPulseCodes.Normal:
                {
                    return "Normal";
                }
            case PedalPulseCodes.AbnormalLeft:
                {
                    return "Abnormal-Left";
                }
            case PedalPulseCodes.AbnormalRight:
                {
                    return "Abnormal-Right";
                }
            case PedalPulseCodes.AbnormalBilateral:
                {
                    return "Abnormal-Bilateral";
                }
            case PedalPulseCodes.NotPerformed:
                {
                    return "Not Performed";
                }
            default:
                {
                    return string.Empty;
                }
        }
    }
}