namespace Signify.Spirometry.Core.Constants.Questions.Performed
{
    /// <summary>
    /// Question in the 'Diagnoses previously recorded for the member' section,
    /// which contains previous diagnoses the member's plan has claimed
    /// </summary>
    /// <remarks>
    /// This question has multiple answers associated with it, many of which the
    /// process manager doesn't care about, such as Last Recorded Visit, Resolved, etc
    /// </remarks>
    public static class DocumentedAndAdditionalDiagnosesQuestion
    {
        public const int QuestionId = 85300;

        /// <remarks>
        /// Note there are usually multiple answers supplied with this AnswerId
        /// for the same evaluation version for the member
        /// </remarks>
        public const int DiagnosesAnswerId = 21925;
    }
}
