using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Services;
using Signify.Spirometry.Core.Validators;

namespace Signify.Spirometry.Core.Converters
{
    /// <summary>
    /// Determines the normality of exam results
    /// </summary>
    public interface IOverallNormalityConverter
    {
        NormalityIndicator Convert(ExamResult examResult);
    }

    /// <inheritdoc />
    public class OverallNormalityConverter : IOverallNormalityConverter
    {
        private const decimal MinNormalRangeInclusive = 0.7m;

        private readonly IFev1FvcRatioValidator _validator;
        private readonly IExamQualityService _examQualityService;

        public OverallNormalityConverter(IFev1FvcRatioValidator validator,
            IExamQualityService examQualityService)
        {
            _validator = validator;
            _examQualityService = examQualityService;
        }

        public NormalityIndicator Convert(ExamResult examResult)
        {
            // See https://wiki.signifyhealth.com/display/AncillarySvcs/Spirometry+Business+Rules

            if (!_examQualityService.IsSufficientQuality(examResult))
                return NormalityIndicator.Undetermined;

            if (!_validator.IsValid(examResult.Fev1FvcRatio))
                return NormalityIndicator.Undetermined;

            return examResult.Fev1FvcRatio >= MinNormalRangeInclusive
                ? NormalityIndicator.Normal
                : NormalityIndicator.Abnormal;
        }
    }
}
