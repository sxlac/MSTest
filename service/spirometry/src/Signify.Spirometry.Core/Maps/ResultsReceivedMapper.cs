using AutoMapper;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events.Akka;
using Signify.Spirometry.Core.Services;
using System;
using System.Collections.Immutable;

namespace Signify.Spirometry.Core.Maps
{
    public class ResultsReceivedMapper :
        ITypeConverter<SpirometryExam, ResultsReceived>,
        ITypeConverter<SpirometryExamResult, ResultsReceived>
    {
        private readonly IExamQualityService _examQualityService;

        public ResultsReceivedMapper(IExamQualityService examQualityService)
        {
            _examQualityService = examQualityService;
        }

        #region Lookups
        private static IImmutableDictionary<TKey, TValue> BuildLookup<TKey, TValue>(TValue entity, Func<TValue, TKey> keySelector) where TValue : IEntityEnum<TValue>
            => entity.GetAllEnumerations().ToImmutableDictionary(keySelector);

        private static IImmutableDictionary<short, NormalityIndicator> _normalities;
        private static IImmutableDictionary<short, NormalityIndicator> Normalities
        {
            get
            {
                return _normalities ??= BuildLookup(NormalityIndicator.Undetermined, n => n.NormalityIndicatorId);
            }
        }

        private static IImmutableDictionary<short, SessionGrade> _sessionGrades;
        private static IImmutableDictionary<short, SessionGrade> SessionGrades
        {
            get
            {
                return _sessionGrades ??= BuildLookup(SessionGrade.A, each => each.SessionGradeId);
            }
        }

        private static IImmutableDictionary<short, OccurrenceFrequency> _frequencies;
        private static IImmutableDictionary<short, OccurrenceFrequency> Frequencies
        {
            get
            {
                return _frequencies ??= BuildLookup(OccurrenceFrequency.Never, each => each.OccurrenceFrequencyId);
            }
        }

        private static IImmutableDictionary<short, TrileanType> _trileanTypes;
        private static IImmutableDictionary<short, TrileanType> TrileanTypes
        {
            get
            {
                return _trileanTypes ??= BuildLookup(TrileanType.Unknown, each => each.TrileanTypeId);
            }
        }
        #endregion Lookups

        public ResultsReceived Convert(SpirometryExam source, ResultsReceived destination, ResolutionContext context)
        {
            destination ??= new ResultsReceived();

            destination.EvaluationId = source.EvaluationId;
            destination.PerformedDate = source.EvaluationCreatedDateTime;
            destination.ReceivedDate = source.EvaluationReceivedDateTime;

            return destination;
        }

        public ResultsReceived Convert(SpirometryExamResult source, ResultsReceived destination, ResolutionContext context)
        {
            destination ??= new ResultsReceived();

            destination.Determination = GetNormality(source.NormalityIndicatorId);

            destination.Results ??= new ExamResultInfo();

            destination.Results.SessionGrade = GetSessionGrade(source.SessionGradeId);

            destination.Results.Fvc = source.Fvc;
            destination.Results.FvcNormality = GetNormality(source.FvcNormalityIndicatorId);

            destination.Results.Fev1 = source.Fev1;
            destination.Results.Fev1Normality = GetNormality(source.Fev1NormalityIndicatorId);

            destination.Results.Fev1OverFvc = source.OverreadFev1FvcRatio ?? source.Fev1FvcRatio;

            SetLungFunctionResults(source, destination.Results);

            destination.Results.Copd = source.CopdDiagnosis;

            destination.Results.EligibleForOverread = _examQualityService.NeedsOverread(source);
            // If we do not know whether a flag is needed yet (ie this returns `null`), then we
            // must wait for overread results to determine if a flag is needed. Therefore, the
            // evaluation is held until the overread is received and we definitively know (or
            // the hold expires).
            destination.Results.WasHeldForOverread = _examQualityService.NeedsFlag(source) ?? true;

            return destination;
        }

        private static void SetLungFunctionResults(SpirometryExamResult source, ExamResultInfo destination)
        {
            destination.HasSmokedTobacco = source.HasSmokedTobacco;
            destination.TotalYearsSmoking = source.TotalYearsSmoking;
            destination.ProducesSputumWithCough = source.ProducesSputumWithCough;

            destination.CoughMucusOccurrenceFrequency = GetFrequency(source.CoughMucusOccurrenceFrequencyId);

            destination.HadWheezingPast12mo = GetTrilean(source.HadWheezingPast12moTrileanTypeId);
            destination.GetsShortnessOfBreathAtRest = GetTrilean(source.GetsShortnessOfBreathAtRestTrileanTypeId);
            destination.GetsShortnessOfBreathWithMildExertion = GetTrilean(source.GetsShortnessOfBreathWithMildExertionTrileanTypeId);

            destination.NoisyChestOccurrenceFrequency = GetFrequency(source.NoisyChestOccurrenceFrequencyId);
            destination.ShortnessOfBreathPhysicalActivityOccurrenceFrequency = GetFrequency(source.ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId);

            destination.LungFunctionScore = source.LungFunctionScore;
        }

        private static string GetFrequency(short? occurrenceFrequencyId)
        {
            return occurrenceFrequencyId.HasValue && Frequencies.TryGetValue(occurrenceFrequencyId.Value, out var frequency)
                ? frequency.Frequency
                : null;
        }

        private static string GetTrilean(short? trileanTypeId)
        {
            return trileanTypeId.HasValue && TrileanTypes.TryGetValue(trileanTypeId.Value, out var trileanType)
                ? trileanType.TrileanValue[..1] // first char, ie 'Y', 'N', 'U'
                : null;
        }

        private static string GetNormality(short normalityIndicatorId)
        {
            return Normalities.TryGetValue(normalityIndicatorId, out var normality)
                ? normality.Indicator.ToString()
                : NormalityIndicator.Undetermined.Indicator.ToString();
        }

        private static string GetSessionGrade(short? sessionGradeId)
        {
            return sessionGradeId.HasValue && SessionGrades.TryGetValue(sessionGradeId.Value, out var sessionGrade)
                ? sessionGrade.SessionGradeCode
                : null;
        }
    }
}
