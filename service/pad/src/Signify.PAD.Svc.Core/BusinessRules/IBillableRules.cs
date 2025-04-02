using Signify.PAD.Svc.Core.Models;

namespace Signify.PAD.Svc.Core.BusinessRules;

public interface IBillableRules
{
    BusinessRuleStatus IsBillable(BillableRuleAnswers answers);
    bool IsNormal(BillableRuleAnswers answers);
    bool IsAbnormal(BillableRuleAnswers answers);
    
}