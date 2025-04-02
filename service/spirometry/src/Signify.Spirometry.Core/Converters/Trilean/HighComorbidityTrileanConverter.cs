using Signify.Spirometry.Core.Constants.Questions.Performed;

namespace Signify.Spirometry.Core.Converters.Trilean
{
    /// <summary>
    /// <see cref="ITrileanTypeConverter"/> implementation for the <see cref="HasHighComorbidityQuestion"/> question
    /// </summary>
    public class HighComorbidityTrileanConverter : TrileanTypeConverter
    {
        public HighComorbidityTrileanConverter() : base(
            HasHighComorbidityQuestion.UnknownAnswerId,
            HasHighComorbidityQuestion.YesAnswerId,
            HasHighComorbidityQuestion.NoAnswerId)
        {
        }
    }
}
