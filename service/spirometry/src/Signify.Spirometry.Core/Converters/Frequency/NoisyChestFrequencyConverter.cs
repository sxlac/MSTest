using Signify.Spirometry.Core.Constants.Questions.Performed;

namespace Signify.Spirometry.Core.Converters.Frequency
{
    /// <summary>
    /// <see cref="IOccurrenceFrequencyConverter"/> implementation for the <see cref="NoisyChestFrequencyQuestion"/> question
    /// </summary>
    public class NoisyChestFrequencyConverter : OccurrenceFrequencyConverter
    {
        public NoisyChestFrequencyConverter() : base(
            NoisyChestFrequencyQuestion.NeverAnswerId,
            NoisyChestFrequencyQuestion.RarelyAnswerId,
            NoisyChestFrequencyQuestion.SometimesAnswerId,
            NoisyChestFrequencyQuestion.OftenAnswerId,
            NoisyChestFrequencyQuestion.VeryOftenAnswerId)
        {
        }
    }
}
