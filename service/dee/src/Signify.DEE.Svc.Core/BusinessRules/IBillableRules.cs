using Signify.DEE.Svc.Core.Messages.Models;

namespace Signify.DEE.Svc.Core.BusinessRules;

public interface IBillableRules
{
    BusinessRuleStatus IsBillable(BillableRuleAnswers answers);
    BusinessRuleStatus IsGradable(BusinessRuleAnswers answers);
    BusinessRuleStatus IsNotGradable(BusinessRuleAnswers answers);
    BusinessRuleStatus IsIncompleteStatusPresent(BusinessRuleAnswers answers);
}