using System;
using System.Collections.Generic;
using System.Text;
using Signify.CKD.Svc.Core.Models;

namespace Signify.CKD.Svc.Core.BusinessRules;

public class BillAndPayRules : IBillableRules, IPayableRules
{
    #region Payable
    /// <summary>
    /// For CKD, ability to pay providers is determined by checking if at least one of the sides in Evaluation Answers are Abnormal or Normal
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    public BusinessRuleStatus IsPayable(PayableRuleAnswers answers)
    {
        var finalStatus = GetCombinedStatus(new List<BusinessRuleStatus>
        {
            IsAnswerExpirationDateAfterDateOfService(answers), IsCkdAnswerValid(answers), IsCkdPerformed(answers)
        });
        return finalStatus;
    }
    #endregion

    #region Billable

    /// <summary>
    /// For CKD, ability to bill clients is determined by checking if at least one of the sides in Evaluation Answers are Abnormal or Normal
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    public BusinessRuleStatus IsBillable(BillableRuleAnswers answers)
    {
        return IsCkdAnswerValid(answers);
    }
    #endregion

    #region DetailedDefinitions
    /// <summary>
    /// Check that ExpirationDate and DateOfService are populated and ExpirationDate is greater
    /// </summary>
    /// <param name="answers"></param>
    /// <returns></returns>
    public BusinessRuleStatus IsAnswerExpirationDateAfterDateOfService(BusinessRuleAnswers answers)
    {
        var status = new BusinessRuleStatus();
        if (!answers.ExpirationDate.HasValue || !answers.DateOfService.HasValue ||
            answers.ExpirationDate.Value == default || answers.DateOfService.Value == default)
        {
            status.Reason = "Invalid ExpirationDate or DateOfService";
        }
        else if (answers.ExpirationDate.Value.Date < answers.DateOfService.Value.Date)
        {
            status.Reason = "ExpirationDate is before DateOfService";
        }
        else
        {
            status.IsMet = true;
        }

        return status;
    }

    /// <summary>
    /// Check that Ckd answers is not null
    /// </summary>
    /// <param name="answers"></param>
    /// <returns></returns>
    private static BusinessRuleStatus IsCkdAnswerValid(BusinessRuleAnswers answers)
    {
        var status = new BusinessRuleStatus();
        if (!string.IsNullOrWhiteSpace(answers.CkdAnswer))
        {
            status.IsMet = true;
        }
        else
        {
            status.Reason = "CKD answer not present";
        }

        return status;
    }

    /// <summary>
    /// Check that Ckd answers is not null
    /// </summary>
    /// <param name="answers"></param>
    /// <returns></returns>
    private static BusinessRuleStatus IsCkdPerformed(BusinessRuleAnswers answers)
    {
        var status = new BusinessRuleStatus();
        if (answers.IsPerformed)
        {
            status.IsMet = true;
        }
        else
        {
            status.Reason = "CKD not performed";
        }

        return status;
    }

    /// <summary>
    /// Builds a <see cref="BusinessRuleStatus"/> from a list of <see cref="BusinessRuleStatus"/>
    /// </summary>
    /// <param name="allStatus"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    /// <exception cref="ArgumentException"></exception>
    private static BusinessRuleStatus GetCombinedStatus(List<BusinessRuleStatus> allStatus)
    {
        if (allStatus.Count == 0)
        {
            throw new ArgumentException($"{nameof(GetCombinedStatus)} should not be invoked with an empty list");
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