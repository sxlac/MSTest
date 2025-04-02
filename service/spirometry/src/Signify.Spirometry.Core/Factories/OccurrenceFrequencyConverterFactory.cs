using Signify.Spirometry.Core.Converters.Frequency;
using System;

namespace Signify.Spirometry.Core.Factories
{
    public class OccurrenceFrequencyConverterFactory : IOccurrenceFrequencyConverterFactory
    {
        /// <inheritdoc />
        public IOccurrenceFrequencyConverter Create(IOccurrenceFrequencyConverterFactory.FrequencyConverterType type)
        {
            return type switch
            {
                IOccurrenceFrequencyConverterFactory.FrequencyConverterType.CoughMucus =>
                    new CoughMucusFrequencyConverter(),
                IOccurrenceFrequencyConverterFactory.FrequencyConverterType.NoisyChest =>
                    new NoisyChestFrequencyConverter(),
                IOccurrenceFrequencyConverterFactory.FrequencyConverterType.ShortnessOfBreathPhysicalActivity =>
                    new ShortnessOfBreathPhysicalActivityFrequencyConverter(),
                _ => throw new NotImplementedException() 
            };
        }
    }
}
