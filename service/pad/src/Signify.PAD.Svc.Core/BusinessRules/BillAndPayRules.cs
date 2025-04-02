using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Models;

namespace Signify.PAD.Svc.Core.BusinessRules;

public class BillAndPayRules : IBillableRules, IPayableRules
{
    #region Payable

    /// <summary>
    /// For PAD, ability to pay providers is determined by checking if at least one of the sides in Evaluation Answers are Abnormal or Normal
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    public BusinessRuleStatus IsPayable(PayableRuleAnswers answers)
    {
        return IsAnswerEitherAbnormalOrNormal(answers);
    }

    #endregion

    #region Billable

    /// <summary>
    /// For PAD, ability to bill clients is determined by checking if at least one of the sides in Evaluation Answers are Abnormal or Normal
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    public BusinessRuleStatus IsBillable(BillableRuleAnswers answers)
    {
        return IsAnswerEitherAbnormalOrNormal(answers);
    }

    /// <summary>
    /// Check if either of the sides in Evaluation Answers are Normal
    /// </summary>
    /// <param name="answers"></param>
    /// <returns></returns>
    public bool IsNormal(BillableRuleAnswers answers)
    {
        return IsAnswerNormal(answers);
    }

    /// <summary>
    /// Check if either of the sides in Evaluation Answers are Abnormal
    /// </summary>
    /// <param name="answers"></param>
    /// <returns></returns>
    public bool IsAbnormal(BillableRuleAnswers answers)
    {
        return IsAnswerAbnormal(answers);
    }

    #endregion

    #region DetailedDefinitions

    /// <summary>
    /// Check if Evaluation Answers are either abnormal or normal with precedence to abnormality
    /// </summary>
    /// <param name="answers"></param>
    /// <returns><see cref="BusinessRuleStatus"/></returns>
    private static BusinessRuleStatus IsAnswerEitherAbnormalOrNormal(BusinessRuleAnswers answers)
    {
        var status = new BusinessRuleStatus();
        if (IsAnswerAbnormal(answers))
        {
            status.IsMet = true;
            return status;
        }

        if (IsAnswerNormal(answers))
        {
            status.IsMet = true;
            return status;
        }

        status.Reason = "Both Left and Right Normality Indicator are Undetermined";
        return status;
    }


    private static bool IsAnswerAbnormal(BusinessRuleAnswers answers) => answers.LeftNormalityIndicator == Application.NormalityIndicator.Abnormal ||
                                                                         answers.RightNormalityIndicator == Application.NormalityIndicator.Abnormal;


    private static bool IsAnswerNormal(BusinessRuleAnswers answers) => answers.LeftNormalityIndicator == Application.NormalityIndicator.Normal ||
                                                                       answers.RightNormalityIndicator == Application.NormalityIndicator.Normal;

    #endregion
}