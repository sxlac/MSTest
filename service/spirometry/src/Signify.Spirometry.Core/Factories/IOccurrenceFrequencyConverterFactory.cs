using Signify.Spirometry.Core.Converters.Frequency;

namespace Signify.Spirometry.Core.Factories
{
    public interface IOccurrenceFrequencyConverterFactory
    {
        public enum FrequencyConverterType
        {
            /// <summary>
            /// Corresponds to the occurrence frequency question <see cref="Signify.Spirometry.Core.Constants.Questions.Performed.CoughMucusFrequencyQuestion"/>
            /// </summary>
            CoughMucus,
            /// <summary>
            /// Corresponds to the occurrence frequency question <see cref="Signify.Spirometry.Core.Constants.Questions.Performed.NoisyChestFrequencyQuestion"/>
            /// </summary>
            NoisyChest,
            /// <summary>
            /// Corresponds to the occurrence frequency question <see cref="Signify.Spirometry.Core.Constants.Questions.Performed.ShortnessOfBreathPhysicalActivityFrequencyQuestion"/>
            /// </summary>
            ShortnessOfBreathPhysicalActivity
        }

        IOccurrenceFrequencyConverter Create(FrequencyConverterType type);
    }
}
