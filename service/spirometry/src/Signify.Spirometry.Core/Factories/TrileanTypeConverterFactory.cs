using Signify.Spirometry.Core.Converters.Trilean;
using System;

namespace Signify.Spirometry.Core.Factories
{
    public class TrileanTypeConverterFactory : ITrileanTypeConverterFactory
    {
        public ITrileanTypeConverter Create(ITrileanTypeConverterFactory.TrileanConverterType type)
        {
            return type switch
            {
                ITrileanTypeConverterFactory.TrileanConverterType.HasEnvOrExpRisk =>
                    new EnvOrExpRiskFactorConverter(),
                ITrileanTypeConverterFactory.TrileanConverterType.HasHighComorbidity =>
                    new HighComorbidityTrileanConverter(),
                ITrileanTypeConverterFactory.TrileanConverterType.HasHighSymptom =>
                    new HighSymptomTrileanConverter(),
                ITrileanTypeConverterFactory.TrileanConverterType.HadWheezingPast12mo =>
                    new HadWheezingPast12moTrileanConverter(),
                ITrileanTypeConverterFactory.TrileanConverterType.GetsShortnessOfBreathAtRest =>
                    new GetsShortnessOfBreathAtRestTrileanConverter(),
                ITrileanTypeConverterFactory.TrileanConverterType.GetsShortnessOfBreathWithMildExertion =>
                    new GetsShortnessOfBreathWithMildExertionTrileanConverter(),
                _ => throw new NotImplementedException()
            };
        }
    }
}
