using Signify.Spirometry.Core.Constants.Questions.Performed;

namespace Signify.Spirometry.Core.Converters.Trilean
{
    /// <summary>
    /// <see cref="ITrileanTypeConverter"/> implementation for the <see cref="GetsShortnessOfBreathWithMildExertionQuestion"/> question
    /// </summary>
    public class GetsShortnessOfBreathWithMildExertionTrileanConverter : TrileanTypeConverter
    {
        public GetsShortnessOfBreathWithMildExertionTrileanConverter() : base(
            GetsShortnessOfBreathWithMildExertionQuestion.UnknownAnswerId,
            GetsShortnessOfBreathWithMildExertionQuestion.YesAnswerId,
            GetsShortnessOfBreathWithMildExertionQuestion.NoAnswerId)
        {
        }
    }
}
