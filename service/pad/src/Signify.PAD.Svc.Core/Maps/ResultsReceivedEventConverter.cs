using AutoMapper;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Models;
using System.Collections.Generic;
using Signify.PAD.Svc.Core.BusinessRules;
using Signify.PAD.Svc.Core.Constants;

namespace Signify.PAD.Svc.Core.Maps
{
    public class ResultsReceivedConverter : ITypeConverter<EvaluationAnswers, ResultsReceived>
    {
        private readonly IBillableRules _billableRules;

        public ResultsReceivedConverter(IBillableRules billableRules)
        {
            _billableRules = billableRules;
        }

        public ResultsReceived Convert(EvaluationAnswers source, ResultsReceived destination, ResolutionContext context)
        {
            destination ??= new ResultsReceived();

            SetOverallNormalityIndicator(destination, source.LeftNormalityIndicator, source.RightNormalityIndicator);
            destination.IsBillable = IsBillable(source.LeftNormalityIndicator, source.RightNormalityIndicator);
            destination.Results = new List<SideResultInfo>();

            destination.Results.Add(new SideResultInfo
            {
                Side = Application.Side.Left,
                Result = source.LeftScore,
                AbnormalIndicator = source.LeftNormalityIndicator,
                Severity = source.LeftSeverity,
                Exception = source.LeftException
            });

            destination.Results.Add(new SideResultInfo
            {
                Side = Application.Side.Right,
                Result = source.RightScore,
                AbnormalIndicator = source.RightNormalityIndicator,
                Severity = source.RightSeverity,
                Exception = source.RightException
            });

            return destination;
        }

        private void SetOverallNormalityIndicator(ResultsReceived destination, string leftIndicator, string rightIndicator)
        {
            var answers = new BillableRuleAnswers { LeftNormalityIndicator = leftIndicator, RightNormalityIndicator = rightIndicator };

            // Abnormal takes precedence
            if (_billableRules.IsAbnormal(answers))
            {
                destination.Determination = Application.NormalityIndicator.Abnormal;
                return;
            }

            // Only one side has to be Normal (the other can be Undetermined) for it to overall be Normal
            if (_billableRules.IsNormal(answers))
            {
                destination.Determination = Application.NormalityIndicator.Normal;
                return;
            }

            destination.Determination = Application.NormalityIndicator.Undetermined;
        }

        /// <summary>
        /// Determine if the exam is billable or not
        /// </summary>
        /// <param name="leftNormalityIndicator"></param>
        /// <param name="rightNormalityIndicator"></param>
        /// <returns></returns>
        private bool IsBillable(string leftNormalityIndicator, string rightNormalityIndicator)
        {
            var answers = new BillableRuleAnswers
                { LeftNormalityIndicator = leftNormalityIndicator, RightNormalityIndicator = rightNormalityIndicator };
            return _billableRules.IsBillable(answers).IsMet;
        }
    }
}