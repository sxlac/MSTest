using System;
using System.Collections.Immutable;
using AutoMapper;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Events.Akka;

namespace Signify.eGFR.Core.Maps;

public class ResultsReceivedMapper
    : ITypeConverter<Exam, ResultsReceived>, ITypeConverter<QuestLabResult, ResultsReceived>, ITypeConverter<LabResult, ResultsReceived>
{
    public ResultsReceived Convert(Exam source, ResultsReceived destination, ResolutionContext context)
    {
        destination ??= new ResultsReceived();

        destination.PerformedDate = source.DateOfService;
        destination.EvaluationId = source.EvaluationId;

        return destination;
    }

    public ResultsReceived Convert(QuestLabResult source, ResultsReceived destination, ResolutionContext context)
    {
        destination ??= new ResultsReceived();
            
        destination.ReceivedDate = source.CreatedDateTime;
        destination.Determination = source.NormalityCode;
        destination.Result = new Group
        {
            Result = source.eGFRResult,
            Description = string.Empty,
            AbnormalIndicator = source.NormalityCode
        };

        return destination;
    }
    
    public ResultsReceived Convert(LabResult source, ResultsReceived destination, ResolutionContext context)
    {
        destination ??= new ResultsReceived();
            
        destination.ReceivedDate = source.CreatedDateTime;
        destination.Determination = GetNormality(source.NormalityIndicatorId);
        destination.Result = new Group
        {
            Result = source.EgfrResult,
            Description = source.ResultDescription,
            AbnormalIndicator = GetNormality(source.NormalityIndicatorId)
        };

        return destination;
    }
    
    #region Lookups
    private static string GetNormality(int normalityIndicatorId)
    {
        return Normalities.TryGetValue(normalityIndicatorId, out var normality)
            ? normality.Indicator.ToString()
            : NormalityIndicator.Undetermined.Indicator.ToString();
    }
    
    private static IImmutableDictionary<TKey, TValue> BuildLookup<TKey, TValue>(TValue entity, Func<TValue, TKey> keySelector) where TValue : IEntityEnum<TValue>
        => entity.GetAllEnumerations().ToImmutableDictionary(keySelector);

    private static IImmutableDictionary<int, NormalityIndicator> _normalities;
    private static IImmutableDictionary<int, NormalityIndicator> Normalities
    {
        get
        {
            return _normalities ??= BuildLookup(NormalityIndicator.Undetermined, n => n.NormalityIndicatorId);
        }
    }
    #endregion Lookups
}