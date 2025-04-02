using Signify.Spirometry.Core.Converters.Trilean;
using Signify.Spirometry.Core.Constants.Questions.Performed;

namespace Signify.Spirometry.Core.Factories
{
    public interface ITrileanTypeConverterFactory
    {
        public enum TrileanConverterType
        {
            /// <summary>
            /// Corresponds to the trilean question <see cref="HasEnvOrExpRiskQuestion"/>
            /// </summary>
            HasEnvOrExpRisk,
            /// <summary>
            /// Corresponds to the trilean question <see cref="HasHighComorbidityQuestion"/>
            /// </summary>
            HasHighComorbidity,
            /// <summary>
            /// Corresponds to the trilean question <see cref="HasHighSymptomsQuestion"/>
            /// </summary>
            HasHighSymptom,
            /// <summary>
            /// Corresponds to the trilean question <see cref="HadWheezingPast12moQuestion"/>
            /// </summary>
            // ReSharper disable once InconsistentNaming
            HadWheezingPast12mo,
            /// <summary>
            /// Corresponds to the trilean question <see cref="GetsShortnessOfBreathAtRestQuestion"/>
            /// </summary>
            GetsShortnessOfBreathAtRest,
            /// <summary>
            /// Corresponds to the trilean question <see cref="GetsShortnessOfBreathWithMildExertionQuestion"/>
            /// </summary>
            GetsShortnessOfBreathWithMildExertion
        }

        ITrileanTypeConverter Create(TrileanConverterType type);
    }
}
