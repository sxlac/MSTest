using AutoMapper;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.Converters;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Validators;
using System;
using System.Linq;

namespace Signify.Spirometry.Core.Maps
{
    public class RawExamResultMapper : ITypeConverter<RawExamResult, ExamResult>
    {
        private readonly ILogger _logger;
        private readonly IGetLoopbackConfig _loopbackConfig;

        private readonly IFvcValidator _fvcValidator;
        private readonly IFev1Validator _fev1Validator;
        private readonly IFev1FvcRatioValidator _fev1FvcRatioValidator;
        private readonly IOverallNormalityConverter _overallNormalityConverter;
        private readonly IFvcNormalityConverter _fvcNormalityConverter;
        private readonly IFev1NormalityConverter _fev1NormalityConverter;

#pragma warning disable S107 // SonarQube: Methods should not have too many parameters
        // This code smell is to help point out classes or methods that may be doing
        // too much, either violating the SRP and/or being too complex. In this case,
        // this class's SRP is simply to map raw exam results to a validated result
        // model, and clearly has both low cognitive, and cyclomatic, complexity.
        // At this point, I see no reason to refactor or further abstract this logic
        // to decrease the number of dependencies by one.
        public RawExamResultMapper(ILogger<RawExamResultMapper> logger,
            IGetLoopbackConfig loopbackConfig,
            IFvcValidator fvcValidator,
            IFev1Validator fev1Validator,
            IFev1FvcRatioValidator fev1FvcRatioValidator,
            IOverallNormalityConverter overallNormalityConverter,
            IFvcNormalityConverter fvcNormalityConverter,
            IFev1NormalityConverter fev1NormalityConverter)
#pragma warning restore S107
        {
            _logger = logger;
            _loopbackConfig = loopbackConfig;

            _fvcValidator = fvcValidator;
            _fev1Validator = fev1Validator;
            _fev1FvcRatioValidator = fev1FvcRatioValidator;
            _overallNormalityConverter = overallNormalityConverter;
            _fvcNormalityConverter = fvcNormalityConverter;
            _fev1NormalityConverter = fev1NormalityConverter;
        }

        public ExamResult Convert(RawExamResult source, ExamResult destination, ResolutionContext context)
        {
            destination ??= new ExamResult();

            destination.SessionGrade = source.SessionGrade;
            destination.HasHighSymptom = source.HasHighSymptom;
            destination.HasEnvOrExpRisk = source.HasEnvOrExpRisk;
            destination.HasHighComorbidity = source.HasHighComorbidity;
            destination.HasSmokedTobacco = source.HasSmokedTobacco;
            destination.TotalYearsSmoking = source.TotalYearsSmoking;
            destination.ProducesSputumWithCough = source.ProducesSputumWithCough;
            destination.CoughMucusFrequency = source.CoughMucusFrequency;
            destination.HadWheezingPast12mo = source.HadWheezingPast12mo;
            destination.GetsShortnessOfBreathAtRest = source.GetsShortnessOfBreathAtRest;
            destination.GetsShortnessOfBreathWithMildExertion = source.GetsShortnessOfBreathWithMildExertion;
            destination.NoisyChestFrequency = source.NoisyChestFrequency;
            destination.ShortnessOfBreathPhysicalActivityFrequency = source.ShortnessOfBreathPhysicalActivityFrequency;
            destination.LungFunctionQuestionnaireScore = source.LungFunctionQuestionnaireScore;
            destination.CopdDiagnosis = source.CopdDiagnosis;

            SetTestResults(source, destination);

            destination.HasHistoryOfCopd = HasHistoryOfCopd(source);

            return destination;
        }

        private void SetTestResults(RawExamResult source, ExamResult destination)
        {
            SetParsedValue(_fvcValidator, source.Fvc, "FVC", fvc => destination.Fvc = fvc);
            SetParsedValue(_fev1Validator, source.Fev1, "FEV1", fev1 => destination.Fev1 = fev1);
            SetParsedValue(_fev1FvcRatioValidator, source.Fev1FvcRatio, "FEV1/FVC", fev1OverFvc => destination.Fev1FvcRatio = fev1OverFvc);

            // Make sure this gets called after the above results are set on the destination
            destination.NormalityIndicator = _overallNormalityConverter.Convert(destination);
            destination.FvcNormalityIndicator = _fvcNormalityConverter.Convert(destination.Fvc);
            destination.Fev1NormalityIndicator = _fev1NormalityConverter.Convert(destination.Fev1);

            void SetParsedValue<T>(IResultValidator<T?> validator, string rawValue, string resultType, Action<T> action)
                where T : struct
            {
                if (!validator.IsValid(rawValue, out var validatedResult))
                {
                    _logger.LogWarning("For EvaluationId={EvaluationId}, {ResultType} result is invalid: {RawResultValue}",
                        source.EvaluationId, resultType, rawValue);
                }

                // Although the supplied result may be invalid according to business rules (ie Out of Range), we still
                // want to set the result to the parsed value if it can be parsed. ie if the raw value is "invalid",
                // this will not parse to an int/decimal, so LogWarn and don't set the result. If the raw value is
                // "500", this is technically invalid (out of range), but we still want to save that value to the db
                // and publish it as-is downstream for reporting.

                // Yes, this may not sound right, but it's what business wants.

                if (validatedResult.HasValue)
                    action(validatedResult.Value);
            }
        }

        private bool? HasHistoryOfCopd(RawExamResult source)
        {
            var copdDiagnosisAnswerValues = _loopbackConfig
                .GetDiagnosisConfigs()
                .Where(diagnosis => diagnosis.Name == Diagnoses.Copd)
                .Select(diagnosis => diagnosis.AnswerValue)
                .ToList();

            if (!copdDiagnosisAnswerValues.Any()) // If there are none, we have no way of knowing
                return null;

            return source.PreviousDiagnoses.Any(d =>
                copdDiagnosisAnswerValues.Contains(d, StringComparer.InvariantCultureIgnoreCase));
        }
    }
}
