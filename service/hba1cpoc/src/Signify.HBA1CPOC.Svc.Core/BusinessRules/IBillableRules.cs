using Signify.HBA1CPOC.Svc.Core.Models;

namespace Signify.HBA1CPOC.Svc.Core.BusinessRules;

public interface IBillableRules
{
    BusinessRuleStatus IsBillable(BillableRuleAnswers answers);
}