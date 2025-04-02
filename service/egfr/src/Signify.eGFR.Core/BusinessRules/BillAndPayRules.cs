using System;
using System.Collections.Generic;
using System.Text;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.Models;

namespace Signify.eGFR.Core.BusinessRules;

public class BillAndPayRules : IBillableRules, IPayableRules
{
    #region Billing

    /// <inheritdoc />
    public BusinessRuleStatus IsBillable(BillableRuleAnswers answers)
    {
        return ValidateNormality(answers);
    }

    #endregion

    #region Payment

    /// <inheritdoc />
    public BusinessRuleStatus IsPayable(PayableRuleAnswers answers)
    {
        return ValidateNormality(answers);
    }

    #endregion

    #region Private

    /// <summary>
    /// Validate if Normality business rules are met
    /// </summary>
    /// <param name="answers"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static BusinessRuleStatus ValidateNormality(BusinessRuleAnswers answers)
    {
        ValidateInputForLabResult(answers, nameof(ValidateNormality));
        ValidateInputForRequiredParams(answers, nameof(ValidateNormality));

        var status = new BusinessRuleStatus();
        switch (answers.NormalityCode)
        {
            case NormalityCodes.Normal:
            case NormalityCodes.Abnormal:
                status.IsMet = true;
                break;
            case NormalityCodes.Undetermined:
                status.Reason = "Normality is Undetermined";
                break;
            default:
                throw new UnableToDetermineBillabilityException(answers.EventId, answers.EvaluationId);
        }

        if (answers.NormalityCode is NormalityCodes.Normal or NormalityCodes.Abnormal)
        {
            status.IsMet = true;
        }
        else
        {
            status.Reason = "Normality is Undetermined";
        }
        
        return status;
    }

    /// <summary>
    /// Validate the input for <see cref="BusinessRuleAnswers"/>
    /// </summary>
    /// <param name="answers">BusinessRuleAnswers</param>
    /// <param name="invokingMethodName">Method for which validation is being done</param>
    /// <exception cref="ArgumentException">If <see cref="BusinessRuleAnswers"/> is null</exception>
    private static void ValidateInputForLabResult(BusinessRuleAnswers answers, string invokingMethodName)
    {
        if (answers is null)
        {
            throw new ArgumentException($"{invokingMethodName} should not be invoked with empty {nameof(BusinessRuleAnswers)}");
        }
    }

    /// <summary>
    /// Validate the input for <see cref="BusinessRuleAnswers.EvaluationId"/> and <see cref="BusinessRuleAnswers.EventId"/>
    /// </summary>
    /// <param name="answers">BusinessRuleAnswers</param>
    /// <param name="invokingMethodName">Method for which validation is being done</param>
    /// <exception cref="ArgumentException">If <see cref="BusinessRuleAnswers.EvaluationId"/> or <see cref="BusinessRuleAnswers.EventId"/> contains default values</exception>
    private static void ValidateInputForRequiredParams(BusinessRuleAnswers answers, string invokingMethodName)
    {
        if (answers is null || answers.EvaluationId == default || answers.EventId == Guid.Empty)
        {
            throw new ArgumentException(
                $"{invokingMethodName} should not be invoked with default {nameof(BusinessRuleAnswers.EvaluationId)} and {nameof(BusinessRuleAnswers.EventId)}");
        }
    }

    /// <summary>
    /// Builds a <see cref="BusinessRuleStatus"/> from a list of <see cref="BusinessRuleStatus"/>.
    /// To be used when there are more than one condition to be checked for Billing or Payment.
    /// </summary>
    /// <param name="allStatus">List of BusinessRuleStatus</param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    /// <exception cref="ArgumentException"></exception>
    private static BusinessRuleStatus GetCombinedStatus(List<BusinessRuleStatus> allStatus)
    {
        if (allStatus.Count == 0)
        {
            throw new ArgumentException($"{nameof(GetCombinedStatus)} should not be invoked with an empty list", nameof(allStatus));
        }

        var reason = new StringBuilder();
        var businessRuleStatus = new BusinessRuleStatus
        {
            IsMet = true
        };
        foreach (var status in allStatus)
        {
            businessRuleStatus.IsMet = businessRuleStatus.IsMet && status.IsMet;
            if (status.IsMet) continue;
            if (reason.Length > 0)
            {
                reason.Append(", ");
            }

            reason.Append(status.Reason);
        }

        var reasonString = reason.ToString();
        businessRuleStatus.Reason = string.IsNullOrWhiteSpace(reasonString) ? null : reasonString;
        return businessRuleStatus;
    }

    #endregion
}