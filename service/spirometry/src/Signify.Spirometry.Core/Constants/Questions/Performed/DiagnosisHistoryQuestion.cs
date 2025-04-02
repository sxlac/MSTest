namespace Signify.Spirometry.Core.Constants.Questions.Performed
{
    /// <summary>
    /// New question that will contain a consolidation of diagnoses that currently
    /// are being saved to the <see cref="ChartReviewDiagnosesQuestion"/> and
    /// <see cref="DocumentedAndAdditionalDiagnosesQuestion"/>
    /// </summary>
    /// <remarks>
    /// This question has multiple answers associated with it, many of which the
    /// process manager doesn't care about, such as Date, Active, Resolved, etc
    /// </remarks>
    public static class DiagnosisHistoryQuestion
    {
        public const int QuestionId = 100496;

        /// <remarks>
        /// Note there are usually multiple answers supplied with this AnswerId
        /// for the same evaluation version for the member
        /// </remarks>
        public const int DiagnosesAnswerId = 52027;
    }
}
