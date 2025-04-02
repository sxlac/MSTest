using Signify.CKD.Svc.Core.Models;

namespace Signify.CKD.Svc.Core.BusinessRules;

public interface IBillableRules
{
    BusinessRuleStatus IsBillable(BillableRuleAnswers answers);
}