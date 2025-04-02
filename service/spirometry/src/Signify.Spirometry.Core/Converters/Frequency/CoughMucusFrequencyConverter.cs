using Signify.Spirometry.Core.Constants.Questions.Performed;

namespace Signify.Spirometry.Core.Converters.Frequency
{
    /// <summary>
    /// <see cref="IOccurrenceFrequencyConverter"/> implementation for the <see cref="CoughMucusFrequencyQuestion"/> question
    /// </summary>
    public class CoughMucusFrequencyConverter : OccurrenceFrequencyConverter
    {
        public CoughMucusFrequencyConverter() : base(
            CoughMucusFrequencyQuestion.NeverAnswerId,
            CoughMucusFrequencyQuestion.RarelyAnswerId,
            CoughMucusFrequencyQuestion.SometimesAnswerId,
            CoughMucusFrequencyQuestion.OftenAnswerId,
            CoughMucusFrequencyQuestion.VeryOftenAnswerId)
        {
        }
    }
}
