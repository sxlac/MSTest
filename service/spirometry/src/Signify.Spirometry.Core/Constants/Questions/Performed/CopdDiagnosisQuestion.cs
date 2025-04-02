namespace Signify.Spirometry.Core.Constants.Questions.Performed
{
    /// <summary>
    /// "Dx: COPD"
    /// </summary>
    /// <remarks>
    /// Note that there are two questions with the same question text and answer text in two
    /// different sections of the form, which therefore have different Question/Answer ID's,
    /// which unfortunately don't always have the same actual answer meaning due to lack of
    /// ability to always keep them in sync in the form rules. ie one may be answered as
    /// "Dx: COPD" but not the other, therefore we must check if there is a Dx assertion in
    /// one and/or the other.
    /// </remarks>
    public static class CopdDiagnosisQuestion
    {
        /// <summary>
        /// Question in the "HEENT & Pulmonary | Assessment" section
        /// </summary>
        public static class Heent
        {
            public const int QuestionId = 268;

            /// <summary>
            /// AnswerId for the answer 'Yes'
            /// </summary>
            /// <remarks>
            /// There is no such answer for 'No'; either a diagnosis is asserted or no diagnosis is made
            /// </remarks>
            public const int YesAnswerId = 20752;
        }

        /// <summary>
        /// Question in the "Diagnostic Studies | Assessment" section
        /// </summary>
        public static class Assessment
        {
            public const int QuestionId = 100307;

            /// <summary>
            /// AnswerId for the answer 'Yes'
            /// </summary>
            /// <remarks>
            /// There is no such answer for 'No'; either a diagnosis is asserted or no diagnosis is made
            /// </remarks>
            public const int YesAnswerId = 50993;
        }
    }
}
