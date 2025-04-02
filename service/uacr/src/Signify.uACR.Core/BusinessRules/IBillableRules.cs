using Signify.uACR.Core.Models;

namespace Signify.uACR.Core.BusinessRules;

public interface IBillableRules
{
    /// <summary>
    /// An exam qualifies for provider payment for uACR if all of the following conditions are true:
    /// (1) Clinically valid result (results received back from vendor) - A clinically valid result is one where
    ///     the determination is either normal or abnormal.
    /// (2) The client has been sent a PDF report of the final evaluation
    /// 
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    BusinessRuleStatus IsBillable(BillableRuleAnswers answers);
}