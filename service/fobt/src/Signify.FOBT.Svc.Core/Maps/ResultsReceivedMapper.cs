using AutoMapper;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using System.Collections.Generic;
using Fobt = Signify.FOBT.Svc.Core.Data.Entities.FOBT;

namespace Signify.FOBT.Svc.Core.Maps
{
    public class ResultsReceivedMapper
        : ITypeConverter<Fobt, Results>, ITypeConverter<LabResults, Results>
    {
        
        public Results Convert(Fobt source, Results destination, ResolutionContext context)
        {
            destination ??= new Results();

            destination.EvaluationId = source.EvaluationId!.Value;

            return destination;
        }

        public Results Convert(LabResults source, Results destination, ResolutionContext context)
        {
            destination ??= new Results();

            destination.PerformedDate = source.ServiceDate;
            destination.ReceivedDate = source.CreatedDateTime;
            destination.Determination = source.AbnormalIndicator;
            destination.Barcode = source.Barcode;
            destination.MemberCollectionDate = source.CollectionDate;
            destination.Result = new List<Group>
            {
                new Group
                {
                    AbnormalIndicator = destination.Determination,
                    Result = source.LabResult,
                    Exception = source.Exception
                }
            };
            
            return destination;
        }
    }
}
