using Signify.uACR.Core.Models;

namespace Signify.uACR.Core.BusinessRules;

public interface IPayableRules
{
    /// <summary>
    /// An exam qualifies for provider payment for uACR if the following condition is true:
    /// Clinically valid result (results received back from vendor) - A clinically valid result is one where
    /// the determination is either normal or abnormal. 
    /// 
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    BusinessRuleStatus IsPayable(PayableRuleAnswers answers);
}

