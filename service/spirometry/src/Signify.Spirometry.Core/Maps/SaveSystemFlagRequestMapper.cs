using AutoMapper;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags.Requests;
using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.Constants.Questions.Performed;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Services.Flags;
using System.Text.Json;

namespace Signify.Spirometry.Core.Maps;

public class SaveSystemFlagRequestMapper :
    ITypeConverter<SpirometryExam, SaveSystemFlagRequest>,
    ITypeConverter<SpirometryExamResult, SaveSystemFlagRequest>
{
    private readonly IFlagTextFormatter _formatter;

    public SaveSystemFlagRequestMapper(IFlagTextFormatter formatter)
    {
        _formatter = formatter;
    }

    public SaveSystemFlagRequest Convert(SpirometryExam source, SaveSystemFlagRequest destination, ResolutionContext context)
    {
        destination ??= new SaveSystemFlagRequest();

        destination.EvaluationId = source.EvaluationId;
        destination.ApplicationId = Application.ApplicationId;

        destination.SystemFlag ??= new PendingCdiSystemFlag();

        destination.SystemFlag.QuestionId = Fev1OverFvcQuestion.QuestionId;
        destination.SystemFlag.AnswerId = Fev1OverFvcQuestion.AnswerId;

        return destination;
    }

    public SaveSystemFlagRequest Convert(SpirometryExamResult source, SaveSystemFlagRequest destination, ResolutionContext context)
    {
        destination ??= new SaveSystemFlagRequest();

        destination.SystemFlag ??= new PendingCdiSystemFlag();

        destination.SystemFlag.Notes = _formatter.FormatFlagText(source);
        destination.SystemFlag.AdminNotes = GetAdminNotes(source);

        return destination;
    }

    private static string GetAdminNotes(SpirometryExamResult source)
    {
        var metaData = new FlagMetaData
        {
            Type = "Spirometry",
            Data = new FlagMetaData.MetaData
            {
                OverreadFev1FvcRatio = source.OverreadFev1FvcRatio
            }
        };

        // Our current use-case is flags only ever get created if the overread results are Abnormal,
        // but those business rules don't belong here in this mapper; handlers determine that
        if (source.NormalityIndicatorId == NormalityIndicator.Abnormal.NormalityIndicatorId)
            metaData.Data.ObstructionPerOverread = true;
        else if (source.NormalityIndicatorId == NormalityIndicator.Normal.NormalityIndicatorId)
            metaData.Data.ObstructionPerOverread = false;
        // else null for Undetermined

        return JsonSerializer.Serialize(metaData);
    }

    /// <summary>
    /// This is deserialized in the IHE app, and then mobile rules are run against the results
    /// to show overread result questions that have read-only answers populated with these values
    /// </summary>
    /// <remarks>
    /// See:
    /// QID 100515 - Obstruction per overread?
    /// QID 100516 - Overread FEV1/FVC value
    /// </remarks>
    private sealed class FlagMetaData
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Local - Needed to serialize
        public string Type { get; set; }

        public MetaData Data { get; set; }

        public class MetaData
        {
            public decimal? OverreadFev1FvcRatio { get; set; }
            public bool? ObstructionPerOverread { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Local
    }
}