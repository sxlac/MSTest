using Signify.FOBT.Svc.Core.Models;

namespace Signify.FOBT.Svc.Core.BusinessRules;

public interface IBillableRules
{
    BusinessRuleStatus IsBillableForResults(BillableRuleAnswers answers);

    BusinessRuleStatus IsLabResultValid(BusinessRuleAnswers answers);
}