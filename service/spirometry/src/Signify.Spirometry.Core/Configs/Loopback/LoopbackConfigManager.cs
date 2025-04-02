using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Signify.Spirometry.Core.Configs.Loopback;

/// <remarks>
/// Should limit the number of places we need to touch if/when we integrate with
/// another solution such as Launch Darkly
/// </remarks>
[ExcludeFromCodeCoverage]
public class LoopbackConfigManager : IGetLoopbackConfig
{
    private readonly LoopbackConfig _config;
    private readonly IApplicationTime _applicationTime;

    private readonly IEnumerable<DiagnosisConfig> _diagnoses;

    /// <inheritdoc />
    public bool ShouldProcessOverreads => _config.ShouldProcessOverreads;

    /// <inheritdoc />
    public bool ShouldCreateFlags => _config.ShouldCreateFlags;

    /// <inheritdoc />
    public bool ShouldReleaseHolds => _config.ShouldReleaseHolds;

    /// <inheritdoc />
    public int HighRiskLfqScoreMaxValue => _config.HighRiskLfqScoreMaxValue;

    /// <inheritdoc />
    public string FlagTextFormat => _config.FlagTextFormat;

    public LoopbackConfigManager(LoopbackConfig config, IApplicationTime applicationTime)
    {
        _config = config;
        _applicationTime = applicationTime;

        var diagnoses = config.Diagnoses.ToArray();
        // ReSharper disable once SimplifyLinqExpressionUseAll - I think this is easier to read
        if (!diagnoses.Any(d => d.Name == Diagnoses.Copd && !string.IsNullOrEmpty(d.AnswerValue))) // Fail fast on startup if we have invalid configuration
            throw new InvalidOperationException($"Configuration does not contain at least one diagnosis answer value for {Diagnoses.Copd}");
        _diagnoses = diagnoses;

        OverreadEvaluationLookupRetryDelay = TimeSpan.FromSeconds(config.OverreadEvaluationLookup.DelayedRetrySeconds);
    }

    /// <inheritdoc />
    public IEnumerable<DiagnosisConfig> GetDiagnosisConfigs()
        => new List<DiagnosisConfig>(_diagnoses);

    /// <inheritdoc />
    public bool IsVersionEnabled(int? formVersionId)
        => formVersionId.HasValue && formVersionId >= _config.FirstSupportedFormVersionId;

    /// <inheritdoc />
    public TimeSpan OverreadEvaluationLookupRetryDelay { get; }

    /// <inheritdoc />
    public bool CanRetryOverreadEvaluationLookup(DateTimeOffset overreadReceivedDateTime)
    {
        if (OverreadEvaluationLookupRetryDelay <= TimeSpan.Zero)
            return false;

        var cutoff = overreadReceivedDateTime.AddSeconds(_config.OverreadEvaluationLookup.RetryLimitSeconds);

        return cutoff > _applicationTime.UtcNow();
    }
}