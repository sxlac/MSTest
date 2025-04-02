using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Models;

using NormalityIndicator = Signify.Spirometry.Core.Data.Entities.NormalityIndicator;
using SessionGrade = Signify.Spirometry.Core.Models.SessionGrade;

namespace Signify.Spirometry.Core.Services
{
    /// <summary>
    /// Interface for analyzing the quality of a spirometry exam
    /// </summary>
    public interface IExamQualityService
    {
        /// <summary>
        /// Determines if the quality of the exam is sufficient
        /// </summary>
        bool IsSufficientQuality(ExamResult examResult);

        /// <summary>
        /// Determines if the quality of the test is insufficient and requires a diagnostic overread
        /// </summary>
        bool NeedsOverread(SpirometryExamResult examResult);

        /// <summary>
        /// Whether a flag is needed for these results
        /// </summary>
        /// <remarks>
        /// Can be <c>null</c> if we cannot definitively determine if a flag is needed yet. This
        /// would be if an overread is required, and we won't know if a flag is required until
        /// the overread is received.
        /// </remarks>
        bool? NeedsFlag(SpirometryExamResult examResult);
    }

    public class ExamQualityService : IExamQualityService
    {
        private readonly ILogger _logger;
        private readonly IGetLoopbackConfig _config;

        public ExamQualityService(ILogger<ExamQualityService> logger,
            IGetLoopbackConfig config)
        {
            _logger = logger;
            _config = config;
        }

        /// <inheritdoc />
        public bool IsSufficientQuality(ExamResult examResult)
            => IsSufficientQuality(new ModelWrapper(examResult));

        private bool IsSufficientQuality(IExamResultWrapper examResult)
        {
            switch (examResult.SessionGrade)
            {
                case SessionGrade.A:
                case SessionGrade.B:
                case SessionGrade.C:
                    // Accuracies of these are of sufficient gradability
                    return true;
                case SessionGrade.D:
                case SessionGrade.E:
                case SessionGrade.F:
                case null: // Session Grade question was not answered
                    return false;
                default:
                    _logger.LogWarning("Unhandled SessionGrade {SessionGrade}, unable to determine quality", examResult.SessionGrade);
                    return false;
            }
        }

        /// <inheritdoc />
        public bool NeedsOverread(SpirometryExamResult examResult)
        {
            if (!examResult.SessionGradeId.HasValue)
                return false;

            return !IsSufficientQuality(new EntityWrapper(examResult));
        }

        private bool IsHighRiskLungFunctionScore(int lungFunctionScore)
            => lungFunctionScore <= _config.HighRiskLfqScoreMaxValue;

        /// <inheritdoc />
        public bool? NeedsFlag(SpirometryExamResult examResult)
        {
            // Flags are only ever applicable when an overread is required
            if (!NeedsOverread(examResult))
                return false;

            // If any of these are `null`, this was finalized before we started processing overreads, so it can't possibly ever have one
            if (!examResult.HasHistoryOfCopd.HasValue || !examResult.LungFunctionScore.HasValue)
                return false;

            if (examResult.CopdDiagnosis is true) // Line 3: Don't hold in the DPS PM if Dx: COPD is asserted
                return false;

            if (examResult.HasHistoryOfCopd is false && !IsHighRiskLungFunctionScore(examResult.LungFunctionScore.Value)) // Scenario 2
                return false;

            // For Scenarios 2, 3 and 4, we will not know whether a flag will be required until we've received the overread
            if (!examResult.OverreadFev1FvcRatio.HasValue)
                return null;

            return examResult.NormalityIndicatorId == NormalityIndicator.Abnormal.NormalityIndicatorId;
        }

        #region Wrappers
        private interface IExamResultWrapper
        {
            SessionGrade? SessionGrade { get; }
        }

        private sealed class ModelWrapper : IExamResultWrapper
        {
            private readonly ExamResult _result;

            public ModelWrapper(ExamResult result)
            {
                _result = result;
            }

            public SessionGrade? SessionGrade => _result.SessionGrade;
        }

        private sealed class EntityWrapper : IExamResultWrapper
        {
            private readonly SpirometryExamResult _result;

            public EntityWrapper(SpirometryExamResult result)
            {
                _result = result;
            }

            public SessionGrade? SessionGrade => (SessionGrade?)_result.SessionGradeId;
        }
        #endregion Wrappers
    }
}
