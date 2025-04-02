using Signify.CKD.Svc.Core.Models;

namespace Signify.CKD.Svc.Core.BusinessRules;

public interface IPayableRules
{
    BusinessRuleStatus IsPayable(PayableRuleAnswers answers);
    BusinessRuleStatus IsAnswerExpirationDateAfterDateOfService(BusinessRuleAnswers answers);
}