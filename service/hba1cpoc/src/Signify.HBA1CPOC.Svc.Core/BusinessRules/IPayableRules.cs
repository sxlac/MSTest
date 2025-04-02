using Signify.HBA1CPOC.Svc.Core.Models;

namespace Signify.HBA1CPOC.Svc.Core.BusinessRules;

public interface IPayableRules
{
    BusinessRuleStatus IsPayable(PayableRuleAnswers answers);
    BusinessRuleStatus IsAnswerExpirationDateAfterDateOfService(BusinessRuleAnswers answers);
}