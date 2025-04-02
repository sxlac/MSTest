using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Validators;

namespace Signify.Spirometry.Core.Converters
{
    /// <remarks>
    /// Since the business rules for individual normality for both FVC and FEV-1 are the same,
    /// I've put the logic here. If/when they ever change individually, this can be removed
    /// and the logic moved to the concrete implementations.
    /// </remarks>
    public abstract class IndividualNormalityConverter
    {
        private readonly IResultValidator<int?> _validator;

        protected IndividualNormalityConverter(IResultValidator<int?> validator)
        {
            _validator = validator;
        }

        public NormalityIndicator Convert(int? result)
        {
            if (!_validator.IsValid(result))
                return NormalityIndicator.Undetermined;

            // See https://wiki.signifyhealth.com/display/AncillarySvcs/Spirometry+Business+Rules

            return result < 80
                ? NormalityIndicator.Abnormal
                : NormalityIndicator.Normal;
        }
    }
}
