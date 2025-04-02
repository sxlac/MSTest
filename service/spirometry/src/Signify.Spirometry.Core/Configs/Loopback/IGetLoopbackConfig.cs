using System;
using System.Collections.Generic;

namespace Signify.Spirometry.Core.Configs.Loopback
{
    /// <summary>
    /// Interface to get configuration around the diagnostic loopback feature
    /// </summary>
    public interface IGetLoopbackConfig
    {
        /// <summary>
        /// Whether or not diagnostic overreads should be processed
        /// </summary>
        bool ShouldProcessOverreads { get; }

        /// <summary>
        /// Whether or not CDI flags should be created to send queries to providers for clarification
        /// </summary>
        bool ShouldCreateFlags { get; }

        /// <summary>
        /// Whether or not to release evaluation holds in CDI
        /// </summary>
        bool ShouldReleaseHolds { get; }

        /// <summary>
        /// Lung Function Questionnaire Score maximum value to still be considered High Risk
        /// </summary>
        int HighRiskLfqScoreMaxValue { get; }

        /// <summary>
        /// Format and text (which supports Markdown) used when creating a flag in CDI, which will
        /// be presented to the provider in a clarification
        /// </summary>
        public string FlagTextFormat { get; }

        /// <summary>
        /// Get the collection of diagnoses required for this process manager
        /// </summary>
        IEnumerable<DiagnosisConfig> GetDiagnosisConfigs();

        /// <summary>
        /// Whether the given Form version supports Dx Loopback
        /// </summary>
        /// <param name="formVersionId">Identifier of the Form version</param>
        bool IsVersionEnabled(int? formVersionId);

        /// <summary>
        /// Number of seconds to delay the retrying of looking up the evaluation associated with an
        /// overread
        /// </summary>
        public TimeSpan OverreadEvaluationLookupRetryDelay { get; }

        /// <summary>
        /// Determines whether to retry looking up the evaluation that is associated with an overread
        /// </summary>
        /// <param name="overreadReceivedDateTime">Timestamp of when the overread was received by Signify</param>
        /// <returns></returns>
        public bool CanRetryOverreadEvaluationLookup(DateTimeOffset overreadReceivedDateTime);
    }
}
