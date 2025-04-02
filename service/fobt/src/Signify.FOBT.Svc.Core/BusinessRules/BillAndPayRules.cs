using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Models;

namespace Signify.FOBT.Svc.Core.BusinessRules;

public class BillAndPayRules : IBillableRules, IPayableRules
{
    public static readonly IReadOnlySet<string> ExcludedStates = new HashSet<string>
    {
        "NY",
        "RI"
    };

    #region Payable

    /// <summary>
    /// For FOBT, ability to pay providers is determined on results received
    ///
    /// When lab results comes in after cdi event check for the latest cdi event that came in 
    ///         * if the latest event was CDIPassed or CDIFailed with pay, provider gets paid
    ///         * if the latest event was CDIFailed without pay, provider does not get paid even if there was an earlier CDIPassed event.
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    public BusinessRuleStatus IsPayable(PayableRuleAnswers answers)
    {
        return IsLabResultValid(answers);
    }

    #endregion

    #region Billable

    /// <summary>
    /// For FOBT, ability to Bill is determined by two Billable events:
    /// (1) FIT Kit is left behind with member
    /// (2) FIT Kit results are received from the vendor and the sample is valid (not Undetermined)
    /// 
    /// If New York or Rhode Island, we cannot bill for Results Received
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    public BusinessRuleStatus IsBillableForResults(BillableRuleAnswers answers)
    {
        return GetCombinedStatus(new List<BusinessRuleStatus>
            { IsAddressBillable(answers), IsLabResultValid(answers) });
    }


    /// <summary>
    /// Signify may not bill clients when results are received for mail-in kits where the
    /// evaluation was performed in NY or RI. For more information, see:
    /// https://wiki.signifyhealth.com/display/AncillarySvcs/FOBT+Business+Rules
    /// 
    /// </summary>
    /// <param name="answers"></param>
    /// <returns></returns>
    /// <exception cref="UnableToDetermineBillabilityException"></exception>
    private static BusinessRuleStatus IsAddressBillable(BillableRuleAnswers answers)
    {
        ValidateForExam(answers, nameof(IsBillableForResults));

        if (string.IsNullOrWhiteSpace(answers.Exam.State))
        {
            throw new UnableToDetermineBillabilityException(
                Convert.ToInt64(answers.Exam.EvaluationId),
                "State information is not available for the Exam"
            );
        }

        var status = new BusinessRuleStatus { IsMet = true };
        if (ExcludedStates.Any(state => state.Equals(answers.Exam.State, StringComparison.OrdinalIgnoreCase)))
        {
            status.IsMet = false;
            status.Reason = $"Exam performed in a state that cannot be billed for: {answers.Exam.State}" + answers.Exam.State;
        }

        return status;
    }

    #endregion

    #region DetailedDefinitions

    /// <summary>
    /// Validate the following:
    /// - if LabResults are passed into the method check if there is a value for Exception field
    /// - else check if IsValidLabResultsReceived is true/false
    /// </summary>
    /// <param name="answers"></param>
    /// <returns></returns>
    public BusinessRuleStatus IsLabResultValid(BusinessRuleAnswers answers)
    {
        ValidateForEitherLabResultsValueOrStatus(answers, nameof(IsLabResultValid));

        var status = new BusinessRuleStatus
        {
            // if LabResults are passed into the method then check value of Exception field, else check if Valid LabResults were received
            IsMet = answers.LabResults is not null
                ? string.IsNullOrWhiteSpace(answers.LabResults.Exception)
                : answers.IsValidLabResultsReceived == true
        };

        status.Reason = status.IsMet ? null : "Exam contains invalid lab results";
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

    /// <summary>
    /// Validate the input for <see cref="BusinessRuleAnswers.LabResults"/>
    /// </summary>
    /// <param name="answers"></param>
    /// <param name="invokingMethodName"></param>
    /// <exception cref="ArgumentException"></exception>
    private static void ValidateForEitherLabResultsValueOrStatus(BusinessRuleAnswers answers, string invokingMethodName)
    {
        if (answers.LabResults is null && answers.IsValidLabResultsReceived is null)
        {
            throw new ArgumentException(
                $"{invokingMethodName} should be invoked with either {nameof(answers.LabResults)} or {nameof(answers.IsValidLabResultsReceived)}");
        }
    }

    /// <summary>
    /// Validate the input for <see cref="BusinessRuleAnswers.Exam"/> value
    /// </summary>
    /// <param name="answers"></param>
    /// <param name="invokingMethodName"></param>
    /// <exception cref="ArgumentException"></exception>
    private static void ValidateForExam(BusinessRuleAnswers answers, string invokingMethodName)
    {
        if (answers.Exam is null)
        {
            throw new ArgumentException($"{invokingMethodName} should not be invoked with null {nameof(answers.Exam)}");
        }
    }

    #endregion
}