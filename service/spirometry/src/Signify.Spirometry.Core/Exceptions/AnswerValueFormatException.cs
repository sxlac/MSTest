using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions
{
    [Serializable]
    public sealed class AnswerValueFormatException : Exception
    {
        /// <summary>
        /// Identifier of the corresponding question
        /// </summary>
        public int QuestionId { get; }

        /// <summary>
        /// Identifier of the answer that was found for this question
        /// </summary>
        public int AnswerId { get; }

        /// <summary>
        /// Actual answer value that is in the wrong format
        /// </summary>
        public string AnswerValue { get; }

        public AnswerValueFormatException(int questionId, int answerId, string answerValue)
            : base($"Invalid answer value format for QuestionId:{questionId}, AnswerId:{answerId}, AnswerValue:{answerValue}")
        {
            QuestionId = questionId;
            AnswerId = answerId;
            AnswerValue = answerValue;
        }

        [ExcludeFromCodeCoverage]
        #region ISerializable
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        private AnswerValueFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion ISerializable
    }
}