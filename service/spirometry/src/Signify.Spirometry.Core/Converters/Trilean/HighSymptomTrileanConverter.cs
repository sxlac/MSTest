using Signify.Spirometry.Core.Constants.Questions.Performed;

namespace Signify.Spirometry.Core.Converters.Trilean
{
    /// <summary>
    /// <see cref="ITrileanTypeConverter"/> implementation for the <see cref="HasHighSymptomsQuestion"/> question
    /// </summary>
    public class HighSymptomTrileanConverter : TrileanTypeConverter
    {
        public HighSymptomTrileanConverter() : base(
            HasHighSymptomsQuestion.UnknownAnswerId,
            HasHighSymptomsQuestion.YesAnswerId,
            HasHighSymptomsQuestion.NoAnswerId)
        {
        }
    }
}
