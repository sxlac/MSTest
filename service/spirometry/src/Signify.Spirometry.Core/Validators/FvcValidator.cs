using Signify.Spirometry.Core.Configs.Exam;

namespace Signify.Spirometry.Core.Validators
{
    /// <summary>
    /// See <a href="https://wiki.signifyhealth.com/display/AncillarySvcs/Spirometry+Business+Rules">Spirometry Business Rules</a>
    /// for validation rules for FVC (Forced Vital Capacity).
    /// </summary>
    public interface IFvcValidator : IResultValidator<int?> { }

    /// <inheritdoc cref="IFvcValidator" />
    public class FvcValidator : IndividualResultValidator, IFvcValidator
    {
        public FvcValidator(IFvcConfig config)
            : base(config) { }
    }
}
