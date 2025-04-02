using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions
{
    [Serializable]
    public sealed class RequiredEvaluationQuestionMissingException : Exception
    {
        /// <summary>
        /// Identifier of the corresponding missing question
        /// </summary>
        public int QuestionId { get; }

        public RequiredEvaluationQuestionMissingException(int questionId)
            : base($"QuestionId:{questionId} is required but was missing")
        {
            QuestionId = questionId;
        }

        [ExcludeFromCodeCoverage]
        #region ISerializable
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        private RequiredEvaluationQuestionMissingException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion ISerializable
    }
}
