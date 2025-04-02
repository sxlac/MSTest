using AutoMapper;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Events.Akka;

namespace Signify.uACR.Core.Maps;

public class ResultsReceivedMapper
    : ITypeConverter<Exam, ResultsReceived>, ITypeConverter<LabResult, ResultsReceived>
{
    public ResultsReceived Convert(Exam source, ResultsReceived destination, ResolutionContext context)
    {
        destination ??= new ResultsReceived();

        destination.PerformedDate = source.DateOfService;
        destination.EvaluationId = source.EvaluationId;

        return destination;
    }

    public ResultsReceived Convert(LabResult source, ResultsReceived destination, ResolutionContext context)
    {
        destination ??= new ResultsReceived();
        
        destination.ReceivedDate = source.CreatedDateTime;
        destination.Determination = source.NormalityCode;
        destination.Result = new Group
        {
            UacrResult = source.UacrResult,
            Description = source.ResultDescription,
            AbnormalIndicator = source.NormalityCode
        };

        return destination;
    }
}