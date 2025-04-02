using Signify.Spirometry.Core.Constants.Questions.Performed;

namespace Signify.Spirometry.Core.Converters.Trilean
{
    /// <summary>
    /// <see cref="ITrileanTypeConverter"/> implementation for the <see cref="HasEnvOrExpRiskQuestion"/> question
    /// </summary>
    public class EnvOrExpRiskFactorConverter : TrileanTypeConverter
    {
        public EnvOrExpRiskFactorConverter() : base(
            HasEnvOrExpRiskQuestion.UnknownAnswerId,
            HasEnvOrExpRiskQuestion.YesAnswerId,
            HasEnvOrExpRiskQuestion.NoAnswerId)
        {
        }
    }
}
