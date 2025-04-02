using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions
{
    [Serializable]
    public sealed class UnsupportedAnswerForQuestionException : Exception
    {
        /// <summary>
        /// Identifier of the question that has an answer selected that is not currently supported
        /// </summary>
        public int QuestionId { get; }

        /// <summary>
        /// Identifier of the unsupported answer
        /// </summary>
        public int AnswerId { get; }

        /// <summary>
        /// Answer value of the unsupported answer
        /// </summary>
        public string AnswerValue { get; }

        public UnsupportedAnswerForQuestionException(int questionId, int answerId, string answerValue)
            : base($"QuestionId:{questionId} has an unsupported AnswerId:{answerId}, with AnswerValue:{answerValue}")
        {
            QuestionId = questionId;
            AnswerId = answerId;
            AnswerValue = answerValue;
        }

        [ExcludeFromCodeCoverage]
        #region ISerializable
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        private UnsupportedAnswerForQuestionException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion ISerializable
    }
}
