using Signify.eGFR.Core.Models;

namespace Signify.eGFR.Core.BusinessRules;

public interface IPayableRules
{
    /// <summary>
    /// For EGFR, an exam is considered Payable if:
    /// (1) Clinically valid result (results received back from vendor)
    /// i.e. Normality is either Normal or Abnormal
    /// 
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    BusinessRuleStatus IsPayable(PayableRuleAnswers answers);
}