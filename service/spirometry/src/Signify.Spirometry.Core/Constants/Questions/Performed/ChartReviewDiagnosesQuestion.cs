namespace Signify.Spirometry.Core.Constants.Questions.Performed
{
    /// <summary>
    /// Question in the Past Diagnoses section, which contains previous diagnoses
    /// Signify has claimed from a previous IHE with the member
    /// </summary>
    /// <remarks>
    /// This question has multiple answers associated with it, many of which the
    /// process manager doesn't care about, such as Date, Active, Resolved, etc
    /// </remarks>
    public static class ChartReviewDiagnosesQuestion
    {
        public const int QuestionId = 89376;

        /// <remarks>
        /// Note there are usually multiple answers supplied with this AnswerId
        /// for the same evaluation version for the member
        /// </remarks>
        public const int DiagnosesAnswerId = 29614;
    }
}
