using Signify.Spirometry.Core.Configs.Exam;
using System;

namespace Signify.Spirometry.Core.Validators
{
    /// <remarks>
    /// Since the business rules for validating individual results for both FVC and FEV-1 are
    /// the same, I've put the logic here. If/when they ever change individually, this can be
    /// removed and the logic moved to the concrete implementations.
    /// </remarks>
    public abstract class IndividualResultValidator : IResultValidator<int?>
    {
        private readonly IIntValueRangeConfig _config;

        protected IndividualResultValidator(IIntValueRangeConfig config)
        {
            if (config.MaxValueInclusive < config.MinValueInclusive)
                throw new ArgumentException($"Configured MaxValueInclusive ({config.MaxValueInclusive}) must be >= MinValueInclusive ({config.MinValueInclusive})");

            _config = config;
        }

        /// <inheritdoc />
        public bool IsValid(string rawValue, out int? validatedResult)
        {
            validatedResult = int.TryParse(rawValue, out var parsed)
                ? parsed : null;

            return IsValid(validatedResult);
        }

        /// <inheritdoc />
        public bool IsValid(int? result)
        {
            return result >= _config.MinValueInclusive && result <= _config.MaxValueInclusive;
        }
    }
}
