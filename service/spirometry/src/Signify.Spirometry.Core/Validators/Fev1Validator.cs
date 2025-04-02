using Signify.Spirometry.Core.Configs.Exam;

namespace Signify.Spirometry.Core.Validators
{
    /// <summary>
    /// See <a href="https://wiki.signifyhealth.com/display/AncillarySvcs/Spirometry+Business+Rules">Spirometry Business Rules</a>
    /// for validation rules for FEV-1 (Forced Expiratory Volume per 1 second).
    /// </summary>
    public interface IFev1Validator : IResultValidator<int?> { }

    /// <inheritdoc cref="IFev1Validator" />
    public class Fev1Validator : IndividualResultValidator, IFev1Validator
    {
        public Fev1Validator(IFev1Config config)
            : base(config) { }
    }
}
