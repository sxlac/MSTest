using Signify.Spirometry.Core.Constants.Questions.Performed;

namespace Signify.Spirometry.Core.Converters.Frequency
{
    /// <summary>
    /// <see cref="IOccurrenceFrequencyConverter"/> implementation for the <see cref="ShortnessOfBreathPhysicalActivityFrequencyQuestion"/> question
    /// </summary>
    public class ShortnessOfBreathPhysicalActivityFrequencyConverter : OccurrenceFrequencyConverter
    {
        public ShortnessOfBreathPhysicalActivityFrequencyConverter() : base(
            ShortnessOfBreathPhysicalActivityFrequencyQuestion.NeverAnswerId,
            ShortnessOfBreathPhysicalActivityFrequencyQuestion.RarelyAnswerId,
            ShortnessOfBreathPhysicalActivityFrequencyQuestion.SometimesAnswerId,
            ShortnessOfBreathPhysicalActivityFrequencyQuestion.OftenAnswerId,
            ShortnessOfBreathPhysicalActivityFrequencyQuestion.VeryOftenAnswerId)
        {
        }
    }
}
