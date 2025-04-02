using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Validators;

namespace Signify.Spirometry.Core.Converters
{
    /// <summary>
    /// Determines the normality of the individual FVC result
    /// </summary>
    public interface IFvcNormalityConverter
    {
        NormalityIndicator Convert(int? result);
    }

    /// <inheritdoc cref="IFvcNormalityConverter" />
    public class FvcNormalityConverter : IndividualNormalityConverter, IFvcNormalityConverter
    {
        public FvcNormalityConverter(IFvcValidator validator)
            : base(validator) { }
    }
}
