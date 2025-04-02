using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Validators;

namespace Signify.Spirometry.Core.Converters
{
    /// <summary>
    /// Determines the normality of the individual FEV-1 result
    /// </summary>
    public interface IFev1NormalityConverter
    {
        NormalityIndicator Convert(int? result);
    }

    /// <inheritdoc cref="IFev1NormalityConverter"/>
    public class Fev1NormalityConverter : IndividualNormalityConverter, IFev1NormalityConverter
    {
        public Fev1NormalityConverter(IFev1Validator validator)
            : base(validator) { }
    }
}
