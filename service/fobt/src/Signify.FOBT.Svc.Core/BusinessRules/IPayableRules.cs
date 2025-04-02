using Signify.FOBT.Svc.Core.Models;

namespace Signify.FOBT.Svc.Core.BusinessRules;

public interface IPayableRules
{
    BusinessRuleStatus IsPayable(PayableRuleAnswers answers);
}