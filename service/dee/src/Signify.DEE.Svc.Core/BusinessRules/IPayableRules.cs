using Signify.DEE.Svc.Core.Messages.Models;

namespace Signify.DEE.Svc.Core.BusinessRules;

public interface IPayableRules
{
    BusinessRuleStatus IsPayable(PayableRuleAnswers answers);
}