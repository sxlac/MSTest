using System;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.Models;

namespace Signify.uACR.Core.BusinessRules;

public class BillAndPayRules : IBillableRules, IPayableRules
{
    #region Billing
    
    /// <inheritdoc />
    public BusinessRuleStatus IsBillable(BillableRuleAnswers answers)
    {
        ValidateInputForRuleEvaluation(answers, nameof(IsBillable));
        return ValidateNormality(answers);
    }
    
    #endregion
    
    #region Billing
    /// <inheritdoc />
    public BusinessRuleStatus IsPayable(PayableRuleAnswers answers)
    {
        ValidateInputForRuleEvaluation(answers, nameof(IsPayable));
        return ValidateNormality(answers);
    }

    #endregion
    
    #region Private
    
    /// <summary>
    /// Validate if Normality business rules are met
    /// </summary>
    /// <param name="answers"></param>
    /// <returns></returns>
    /// <exception cref="UnableToDetermineBillabilityException"></exception>
    private static BusinessRuleStatus ValidateNormality(BusinessRuleAnswers answers)
    {
        var status = new BusinessRuleStatus();
        switch (answers.Result.NormalityCode)
        {
            case NormalityCodes.Normal:
            case NormalityCodes.Abnormal:
                status.IsMet = true;
                break;
            case NormalityCodes.Undetermined:
                status.IsMet = false;
                status.Reason = "Normality is Undetermined";
                break;
            default:
                throw new UnableToDetermineBillabilityException(answers.EventId, answers.EvaluationId);
        }
        return status;
    }
    
    /// <summary>
    /// Validate the input for <see cref="BusinessRuleAnswers.Result"/>, <see cref="BusinessRuleAnswers.EvaluationId"/> and <see cref="BusinessRuleAnswers.EventId"/>
    /// </summary>
    /// <param name="answers">BusinessRuleAnswers</param>
    /// <param name="invokingMethodName">Method for which validation is being done</param>
    /// <exception cref="ArgumentException">If <see cref="BusinessRuleAnswers.Result"/> is null</exception>
    /// <exception cref="ArgumentException">If <see cref="BusinessRuleAnswers.EvaluationId"/> or <see cref="BusinessRuleAnswers.EventId"/> contains default values</exception>
    private static void ValidateInputForRuleEvaluation(BusinessRuleAnswers answers, string invokingMethodName)
    {
        if (answers is null)
        {
            throw new ArgumentException(
                $"{invokingMethodName} should not be invoked with no {nameof(BusinessRuleAnswers)}");
        } 
        
        if (answers.Result is null)
        {
            throw new ArgumentException($"{invokingMethodName} should not be invoked with empty {nameof(BusinessRuleAnswers.Result)}");
        }
        
        if (answers.EvaluationId == default || answers.EventId == Guid.Empty)
        {
            throw new ArgumentException(
                $"{invokingMethodName} should not be invoked with default {nameof(BusinessRuleAnswers.EvaluationId)} and {nameof(BusinessRuleAnswers.EventId)}");
        }
    }
    
    #endregion
}