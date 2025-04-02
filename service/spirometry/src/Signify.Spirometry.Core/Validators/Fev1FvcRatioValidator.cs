namespace Signify.Spirometry.Core.Validators
{
    /// <summary>
    /// See <a href="https://wiki.signifyhealth.com/display/AncillarySvcs/Spirometry+Business+Rules">Spirometry Business Rules</a>
    /// for validation rules for FEV-1/FVC
    /// </summary>
    public interface IFev1FvcRatioValidator : IResultValidator<decimal?> { }

    /// <inheritdoc />
    public class Fev1FvcRatioValidator : IFev1FvcRatioValidator
    {
        private const decimal MinRangeExclusive = 0m;
        private const decimal MaxRangeInclusive = 1m;

        /// <inheritdoc />
        public bool IsValid(string rawValue, out decimal? validatedResult)
        {
            validatedResult = decimal.TryParse(rawValue, out var parsed)
                ? parsed : null;

            return IsValid(parsed);
        }

        /// <inheritdoc />
        public bool IsValid(decimal? fev1OverFvc)
        {
            return fev1OverFvc is > MinRangeExclusive and <= MaxRangeInclusive;
        }
    }
}
