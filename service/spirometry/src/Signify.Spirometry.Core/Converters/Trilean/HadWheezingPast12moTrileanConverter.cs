using Signify.Spirometry.Core.Constants.Questions.Performed;

// ReSharper disable InconsistentNaming

namespace Signify.Spirometry.Core.Converters.Trilean
{
    /// <summary>
    /// <see cref="ITrileanTypeConverter"/> implementation for the <see cref="HadWheezingPast12moQuestion"/> question
    /// </summary>
    #pragma warning disable S101 // SonarQube - Types should be named in PascalCase - "12mo" is used in English more than "12Mo"
    public class HadWheezingPast12moTrileanConverter : TrileanTypeConverter
    #pragma warning restore S101
    {
        public HadWheezingPast12moTrileanConverter() : base(
            HadWheezingPast12moQuestion.UnknownAnswerId,
            HadWheezingPast12moQuestion.YesAnswerId,
            HadWheezingPast12moQuestion.NoAnswerId)
        {
        }
    }
}
