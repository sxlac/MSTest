using Signify.PAD.Svc.Core.Models;

namespace Signify.PAD.Svc.Core.BusinessRules;

public interface IPayableRules
{
    BusinessRuleStatus IsPayable(PayableRuleAnswers answers);
}