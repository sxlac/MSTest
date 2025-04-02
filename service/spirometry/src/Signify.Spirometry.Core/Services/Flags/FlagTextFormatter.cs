using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Data.Entities;

namespace Signify.Spirometry.Core.Services.Flags
{
    /// <inheritdoc />
    public class FlagTextFormatter : IFlagTextFormatter
    {
        private const string OverreadRatio = "<<overread-ratio>>";
        private const string HighRiskLfqMaxValue = "<<high-risk-lfq-max-value>>";

        private readonly IGetLoopbackConfig _config;

        public FlagTextFormatter(ILogger<FlagTextFormatter> logger, IGetLoopbackConfig config)
        {
            _config = config;

            logger.LogInformation("Using HighRiskLfqScoreMaxValue={HighRiskLfqScoreMaxValue}, and flag text format {Format}",
                config.HighRiskLfqScoreMaxValue, config.FlagTextFormat);
        }

        /// <inheritdoc />
        public string FormatFlagText(SpirometryExamResult result)
        {
            return _config.FlagTextFormat
                .Replace(OverreadRatio, result.OverreadFev1FvcRatio?.ToString())
                .Replace(HighRiskLfqMaxValue, _config.HighRiskLfqScoreMaxValue.ToString())
                // We need to un-escape these because we don't want them escaped when it reaches Mobile, or it won't format correctly
                .Replace(@"\n", "\n")
                .Replace(@"\r", "\r")
                .Replace(@"\t", "\t");
        }
    }
}
