// ReSharper disable InconsistentNaming
namespace Signify.Spirometry.Core.Constants.Questions.Performed
{
    /// <summary>
    /// Have you had wheezing in the past 12 months?
    /// </summary>
    #pragma warning disable S101 // SonarQube - Types should be named in PascalCase - "12mo" is used in English more than "12Mo"
    public static class HadWheezingPast12moQuestion
    #pragma warning restore S101
    {
        public const int QuestionId = 87;

        public const int YesAnswerId = 20484;

        public const int NoAnswerId = 20483;

        public const int UnknownAnswerId = 33594;
    }
}
