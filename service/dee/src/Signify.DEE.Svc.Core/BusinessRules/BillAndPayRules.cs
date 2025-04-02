using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iris.Public.Types.Models;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;

namespace Signify.DEE.Svc.Core.BusinessRules;

public class BillAndPayRules : IBillableRules, IPayableRules
{
    #region Billing

    /// <summary>
    /// Check if an exam is billable
    /// </summary>
    /// <param name="answers"></param>
    /// <returns></returns>
    public BusinessRuleStatus IsBillable(BillableRuleAnswers answers)
    {
        if (answers.Gradings is not null)
        {
            return GetCombinedStatus(new List<BusinessRuleStatus>
            {
                IsGradable(answers), ValidateImageDetails(answers.ImageDetails, answers.HasEnucleation)
            });
        }

        if (answers.StatusCodes.Count > 0)
        {
            return GetCombinedStatus(new List<BusinessRuleStatus>
            {
                IsGradable(answers), ValidateImageDetails(answers.StatusCodes)
            });
        }

        return new BusinessRuleStatus
        {
            Reason = "Invalid data"
        };
    }

    #endregion

    #region Payment

    /// <summary>
    /// Check if an exam is payable
    /// </summary>
    /// <param name="answers"></param>
    /// <returns></returns>
    public BusinessRuleStatus IsPayable(PayableRuleAnswers answers)
    {
        if (answers.Gradings is not null)
        {
            return GetCombinedStatus(new List<BusinessRuleStatus>
            {
                ValidateFindings(answers.Gradings), ValidateImageDetails(answers.ImageDetails, answers.HasEnucleation)
            });
        }

        if (answers.StatusCodes?.Count > 0)
        {
            return GetCombinedStatus(new List<BusinessRuleStatus>
            {
                ValidateFindings(answers.StatusCodes), ValidateImageDetails(answers.StatusCodes)
            });
        }

        return new BusinessRuleStatus
        {
            Reason = "Invalid data"
        };
    }

    #endregion

    #region Common

    public BusinessRuleStatus IsGradable(BusinessRuleAnswers answers)
    {
        if (answers.Gradings is not null)
        {
            return ValidateFindings(answers.Gradings);
        }

        if (answers.StatusCodes?.Count > 0)
        {
            return ValidateFindings(answers.StatusCodes);
        }

        return new BusinessRuleStatus
        {
            Reason = "Invalid data"
        };
    }

    public BusinessRuleStatus IsNotGradable(BusinessRuleAnswers answers)
    {
        if (answers.StatusCodes?.Count == 0)
        {
            throw new ArgumentException($"{nameof(IsNotGradable)} should not be invoked with invalid StatusCodes");
        }

        var status = new BusinessRuleStatus();
        if (answers.StatusCodes is not null && answers.StatusCodes.Contains((int)ExamStatusCode.StatusCodes.NotGradable))
        {
            status.IsMet = true;
        }

        return status;
    }

    public BusinessRuleStatus IsIncompleteStatusPresent(BusinessRuleAnswers answers)
    {
        if (answers.StatusCodes?.Count == 0)
        {
            throw new ArgumentException($"{nameof(IsIncompleteStatusPresent)} should not be invoked with invalid StatusCodes");
        }

        var status = new BusinessRuleStatus();
        if (answers.StatusCodes is not null && answers.StatusCodes.Contains((int)ExamStatusCode.StatusCodes.Incomplete))
        {
            status.IsMet = true;
        }

        return status;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Check if there are any findings for any side based on the gradings
    /// </summary>
    /// <param name="gradings"><see cref="ResultGrading"/></param>
    /// <returns></returns>
    private static BusinessRuleStatus ValidateFindings(ResultGrading gradings)
    {
        var status = new BusinessRuleStatus();

        if ((gradings?.OD?.Findings != null && gradings.OD.Findings.Any()) ||
            (gradings?.OS?.Findings != null && gradings.OS.Findings.Any()))
        {
            status.IsMet = true;
        }
        else
        {
            status.Reason = "Exam is not gradable";
        }

        return status;
    }

    /// <summary>
    /// Check if there are any findings for any side based on the presence of Gradable status in ExamStatus table
    /// </summary>
    /// <param name="statusCodes"></param>
    /// <returns></returns>
    private static BusinessRuleStatus ValidateFindings(IEnumerable<int> statusCodes)
    {
        var status = new BusinessRuleStatus();
        if (statusCodes.Contains((int)ExamStatusCode.StatusCodes.Gradable))
        {
            status.IsMet = true;
        }
        else
        {
            status.Reason = "Exam is not gradable";
        }

        return status;
    }

    /// <summary>
    /// Check if at least one side has images based on imageDetails
    /// </summary>
    /// <param name="imageDetails"><exception cref="ResultImageDetails"></exception></param>
    /// <returns></returns>
    private static BusinessRuleStatus ValidateImageDetails(ResultImageDetails imageDetails, bool hasEnucleation)
    {
        var status = new BusinessRuleStatus();

        int leftCount = imageDetails?.LeftEyeOriginalCount ?? 0;
        int rightCount = imageDetails?.RightEyeOriginalCount ?? 0;

        if (!hasEnucleation)
        {
            if (leftCount > 0 && rightCount > 0)
            {
                status.IsMet = true;
            }
            else
            {
                status.Reason = "At least one eye does not contain images";
            }
        }
        else
        {
            // If one eye is enucleated, then we only need at least one eye with images.
            if (leftCount > 0 || rightCount > 0)
            {
                status.IsMet = true;
            }
            else
            {
                status.Reason = "No images present for the non-enucleated eye";
            }
        }

        return status;
    }

    /// <summary>
    ///  Check if at least one side has images based on absence of Incomplete status in ExamStatus table
    /// </summary>
    /// <param name="statusCodes"></param>
    /// <returns></returns>
    private static BusinessRuleStatus ValidateImageDetails(IEnumerable<int> statusCodes)
    {
        var status = new BusinessRuleStatus();
        if (statusCodes.All(s => s != (int)ExamStatusCode.StatusCodes.Incomplete))
        {
            status.IsMet = true;
        }
        else
        {
            status.Reason = "At least one eye does not contain images";
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
}

#endregion