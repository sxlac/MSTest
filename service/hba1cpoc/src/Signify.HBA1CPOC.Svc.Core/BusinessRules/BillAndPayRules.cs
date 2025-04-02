using System;
using System.Collections.Generic;
using System.Text;
using Signify.HBA1CPOC.Svc.Core.Models;

namespace Signify.HBA1CPOC.Svc.Core.BusinessRules;

public class BillAndPayRules : IBillableRules, IPayableRules
{
    #region Payable

    /// <summary>
    /// For HBA1cPOC, ability to pay providers is determined by checking “Expiration Date (A1cNow)" is on or after DateOfService
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    public BusinessRuleStatus IsPayable(PayableRuleAnswers answers)
    {
        var finalStatus = GetCombinedStatus(new List<BusinessRuleStatus>
        {
            IsAnswerEitherAbnormalOrNormal(answers), IsAnswerExpirationDateAfterDateOfService(answers)
        });
        return finalStatus;
    }

    #endregion

    #region Billable

    /// <summary>
    /// For HBA1cPOC, ability to Bill is determined by checking Normality - Is Normality Normal or Abnormal
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    public BusinessRuleStatus IsBillable(BillableRuleAnswers answers)
    {
        return IsAnswerEitherAbnormalOrNormal(answers);
    }

    #endregion

    #region DetailedDefinitions

    /// <summary>
    /// Check that ExpirationDate and DateOfService are populated and ExpirationDate is greater
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    public BusinessRuleStatus IsAnswerExpirationDateAfterDateOfService(BusinessRuleAnswers answers)
    {
        var status = new BusinessRuleStatus();
        if (!answers.ExpirationDate.HasValue || !answers.DateOfService.HasValue ||
            answers.ExpirationDate.Value == default || answers.DateOfService.Value == default)
        {
            status.Reason = "Invalid ExpirationDate or DateOfService";
        }
        else if (answers.ExpirationDate.Value < DateOnly.FromDateTime(answers.DateOfService.Value.Date))
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
    /// Check normality indicator 
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    /// <exception cref="ArgumentException"></exception>
    private static BusinessRuleStatus IsAnswerEitherAbnormalOrNormal(BusinessRuleAnswers answers)
    {
        var status = new BusinessRuleStatus();
        switch (answers.NormalityIndicator)
        {
            case Normality.Normal:
            case Normality.Abnormal:
                status.IsMet = true;
                break;
            case Normality.Undetermined:
                status.Reason = "Normality is Undetermined";
                break;
            default:
                throw new ArgumentException("Unhandled normality");
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