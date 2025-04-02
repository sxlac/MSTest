using AutoMapper;
using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Svc.Core.BusinessRules;

namespace Signify.HBA1CPOC.Svc.Core.Maps
{
    public class ResultsReceivedMapper : ITypeConverter<ResultsModel, ResultsReceived>
    {
        private readonly IBillableRules _billableRules;

        public ResultsReceivedMapper(IBillableRules billableRules)
        {
            _billableRules = billableRules;
        }

        public ResultsReceived Convert(ResultsModel source, ResultsReceived destination, ResolutionContext context)
        {
            destination ??= new ResultsReceived();


            destination.Determination = context.Mapper.Map<string>(source.Normality);

            destination.Results = new ResultInfo
            {
                Result = source.RawValue,
                AbnormalIndicator = destination.Determination,
                Exception = source.Exception
            };

            destination.IsBillable = IsBillable(source.Normality);
            return destination;
        }

        /// <summary>
        /// Determine if the exam is billable by rules defined in IBillableRules 
        /// </summary>
        /// <param name="normality"></param>
        /// <returns></returns>
        private bool IsBillable(Normality normality)
        {
            var answers = new BillableRuleAnswers { NormalityIndicator = normality };
            return _billableRules.IsBillable(answers).IsMet;
        }
    }
}