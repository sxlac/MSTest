using Signify.Spirometry.Core.Constants.Questions.Performed;

namespace Signify.Spirometry.Core.Converters.Trilean
{
    /// <summary>
    /// <see cref="ITrileanTypeConverter"/> implementation for the <see cref="GetsShortnessOfBreathAtRestQuestion"/> question
    /// </summary>
    public class GetsShortnessOfBreathAtRestTrileanConverter : TrileanTypeConverter
    {
        public GetsShortnessOfBreathAtRestTrileanConverter() : base(
            GetsShortnessOfBreathAtRestQuestion.UnknownAnswerId,
            GetsShortnessOfBreathAtRestQuestion.YesAnswerId,
            GetsShortnessOfBreathAtRestQuestion.NoAnswerId)
        {
        }
    }
}
