using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Signify.uACR.Core.ApiClients.EvaluationApi.Responses;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Constants.Questions;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.Models;
using NotPerformedReason = Signify.uACR.Core.Models.NotPerformedReason;

namespace Signify.uACR.Core.Builders;

public class ExamModelBuilder : IExamModelBuilder
{
    private readonly ILogger _logger;
    private readonly long _evaluationId;
    private readonly int _formVersionId;
    private readonly IDictionary<int, EvaluationAnswerModel> _answers;
    private readonly long _providerId;

    public ExamModelBuilder(ILogger<ExamModelBuilder> logger)
    {
        _logger = logger;
    }

    private ExamModelBuilder(ILogger logger,
        long evaluationId,
        IDictionary<int, EvaluationAnswerModel> answers,
        int formVersionId,
        long providerId)
    {
        _logger = logger;
        _evaluationId = evaluationId;
        _answers = answers;
        _formVersionId = formVersionId;
        _providerId = providerId;
    }

    public IExamModelBuilder ForEvaluation(long evaluationId)
    {
        if (evaluationId < 1)
            throw new ArgumentOutOfRangeException(nameof(evaluationId), evaluationId, $"{nameof(evaluationId)} must be positive");

        return new ExamModelBuilder(_logger,
            evaluationId,
            _answers != null ? new Dictionary<int, EvaluationAnswerModel>(_answers) : null, _formVersionId, _providerId);
    }

    public IExamModelBuilder WithFormVersion(int formVersionId)
    {
        if (formVersionId < 1)
            throw new ArgumentOutOfRangeException(nameof(formVersionId), formVersionId, $"{nameof(formVersionId)} must be positive");

        return new ExamModelBuilder(_logger, _evaluationId, _answers, formVersionId, _providerId);
    }

    public IExamModelBuilder WithAnswers(IEnumerable<EvaluationAnswerModel> answers)
    {
        ArgumentNullException.ThrowIfNull(answers);

        var dict = new Dictionary<int, EvaluationAnswerModel>();
        foreach (var answer in answers)
        {
            // At least for uACR, we don't have any Q's that can be answered more than once
            dict.TryAdd(answer.QuestionId, answer);
        }

        // An IExamModelBuilder will be supplied to consumers via DI. By returning a new ExamModelBuilder here,
        // we can simply treat the ExamModelBuilder in the DI container as simply a singleton.
        return new ExamModelBuilder(_logger, _evaluationId, dict, _formVersionId, _providerId);
    }

    public IExamModelBuilder WithProviderId(long providerId)
    {
        if (providerId < 1)
            throw new ArgumentOutOfRangeException(nameof(providerId), providerId, $"{nameof(providerId)} must be positive");

        return new ExamModelBuilder(_logger, _evaluationId, _answers, _formVersionId, providerId);
    }

    public ExamModel Build()
    {
        if (_evaluationId < 1)
            throw new InvalidOperationException("Must pass evaluation id to ForEvaluation method first");
        if (_answers == null)
            throw new InvalidOperationException("Must pass answers to WithAnswers method first");

        _logger.LogInformation("For EvaluationId={EvaluationId}, for FormVersionId={FormVersionId}",
            _evaluationId, _formVersionId);

        //check for KED question ids
        var kedTestPerformedQ = GetRequired(KedTestPerformedQuestion.QuestionId);

        if (kedTestPerformedQ.AnswerId == KedTestPerformedQuestion.NoAnswerId)
        {
            return new ExamModel(_evaluationId, ParseNotPerformed(KedReasonNotPerformedQuestion.QuestionId), GetReasonNotes());
        }

        //Check for valid uACR question ids
        var uAcrTestPerformedQ = GetRequired(TestPerformedQuestion.QuestionId);

        switch (uAcrTestPerformedQ.AnswerId)
        {
            case TestPerformedQuestion.YesAnswerId:
                return new ExamModel(_evaluationId, ParsePerformed());
            case TestPerformedQuestion.NoAnswerId:
                return new ExamModel(_evaluationId, ParseNotPerformed(ReasonNotPerformedQuestion.QuestionId), GetReasonNotes());
            default:
                _logger.LogWarning("uACR not yet configured to process not performed evaluations");
                break;
        }

        return new ExamModel(1);
    }

    #region Performed
    private RawExamResult ParsePerformed()
    {
        var numericalBarcode = GetNumericalBarcode();
        var alphaBarcode = GetAlphaBarcode();
        var results = new RawExamResult
        {
            EvaluationId = _evaluationId,
            Barcode = $"{numericalBarcode}|{alphaBarcode}",
            ValidBarcode = ValidateBarcode(numericalBarcode, alphaBarcode)
        };

        return results;
    }

    private static bool ValidateBarcode(string numericalBarcode, string alphaBarcode)
    {
        return ValidateNumericBarcode(numericalBarcode) && ValidateAlphaBarcode(alphaBarcode);
    }

    private string GetNumericalBarcode()
    {
        //try and get both barcodes for LetsGetChecked
        if (!TryGetRequired(KedNumericalCode.QuestionId, Application.Barcode, out var numericalBarCodeModel))
            throw new BarcodeNotFoundException(_evaluationId, _formVersionId, KedNumericalCode.QuestionId,
                KedNumericalCode.AnswerId, _providerId);
        
        return string.IsNullOrWhiteSpace(numericalBarCodeModel.AnswerValue)
            ? throw new BarcodeNotFoundException(_evaluationId, _formVersionId, KedNumericalCode.QuestionId,
                KedNumericalCode.AnswerId, _providerId)
            : numericalBarCodeModel.AnswerValue.ToUpper();
    }

    private string GetAlphaBarcode()
    {
        //try and get both barcodes for LetsGetChecked
        if (!TryGetRequired(KedAlphaBarcode.QuestionId, Application.Barcode, out var alphaBarCodeModel))
            throw new BarcodeNotFoundException(_evaluationId, _formVersionId, KedAlphaBarcode.QuestionId,
                KedAlphaBarcode.AnswerId, _providerId);

        return string.IsNullOrWhiteSpace(alphaBarCodeModel.AnswerValue)
            ? throw new BarcodeNotFoundException(_evaluationId, _formVersionId, KedAlphaBarcode.QuestionId,
                KedAlphaBarcode.AnswerId, _providerId)
            : alphaBarCodeModel.AnswerValue.ToUpper();
    }

    #endregion Performed

    #region Not Performed

    private NotPerformedReason ParseNotPerformed(int questionId)
    {
        var q = GetRequired(questionId);
        return q.AnswerId switch
        {
            //KED not performed answer checks
            KedReasonNotPerformedQuestion.MemberRefusedAnswerId => GetMemberRefusedResult(KedReasonMemberRefusedQuestion.QuestionId),
            KedReasonNotPerformedQuestion.ProviderUnableToPerform => GetProviderUnableToPerformResult(KedReasonProviderUnableToPerformQuestion.QuestionId),
            
            ReasonNotPerformedQuestion.MemberRefusedAnswerId => GetMemberRefusedResult(ReasonMemberRefusedQuestion.QuestionId),
            ReasonNotPerformedQuestion.ProviderUnableToPerform => GetProviderUnableToPerformResult(ReasonProviderUnableToPerformQuestion.QuestionId),
            _ => throw new UnsupportedAnswerForQuestionException(_evaluationId, _formVersionId, questionId, q.AnswerId, q.AnswerValue, _providerId)
        };
    }

    private NotPerformedReason GetMemberRefusedResult(int questionId)
    {
        var q = GetRequired(questionId);

        return q.AnswerId switch
        {
            //KED member refused answer checks
            KedReasonMemberRefusedQuestion.ScheduledToCompleteAnswerId => NotPerformedReason.ScheduledToComplete,
            KedReasonMemberRefusedQuestion.MemberApprehensionAnswerId => NotPerformedReason.MemberApprehension,
            KedReasonMemberRefusedQuestion.NotInterestedAnswerId => NotPerformedReason.NotInterested,
            KedReasonMemberRefusedQuestion.MemberRecentlyCompletedAnswerId => NotPerformedReason.MemberRecentlyCompleted,
            
            ReasonMemberRefusedQuestion.ScheduledToCompleteAnswerId => NotPerformedReason.ScheduledToComplete,
            ReasonMemberRefusedQuestion.MemberApprehensionAnswerId => NotPerformedReason.MemberApprehension,
            ReasonMemberRefusedQuestion.NotInterestedAnswerId => NotPerformedReason.NotInterested,
            ReasonMemberRefusedQuestion.MemberRecentlyCompletedAnswerId => NotPerformedReason.MemberRecentlyCompleted,
            _ => throw new UnsupportedAnswerForQuestionException(_evaluationId, _formVersionId, questionId, q.AnswerId, q.AnswerValue, _providerId)
        };
    }

    private NotPerformedReason GetProviderUnableToPerformResult(int questionId)
    {
        var q = GetRequired(questionId);

        return q.AnswerId switch
        {
            //KED provider unable to perform answer checks
            KedReasonProviderUnableToPerformQuestion.TechnicalIssueAnswerId => NotPerformedReason.TechnicalIssue,
            KedReasonProviderUnableToPerformQuestion.EnvironmentalIssueAnswerId => NotPerformedReason.EnvironmentalIssue,
            KedReasonProviderUnableToPerformQuestion.NoSuppliesOrEquipmentAnswerId => NotPerformedReason.NoSuppliesOrEquipment,
            KedReasonProviderUnableToPerformQuestion.InsufficientTrainingAnswerId => NotPerformedReason.InsufficientTraining,
            KedReasonProviderUnableToPerformQuestion.MemberPhysicallyUnableAnswerId => NotPerformedReason.MemberPhysicallyUnable,
            
            ReasonProviderUnableToPerformQuestion.TechnicalIssueAnswerId => NotPerformedReason.TechnicalIssue,
            ReasonProviderUnableToPerformQuestion.EnvironmentalIssueAnswerId => NotPerformedReason.EnvironmentalIssue,
            ReasonProviderUnableToPerformQuestion.NoSuppliesOrEquipmentAnswerId => NotPerformedReason.NoSuppliesOrEquipment,
            ReasonProviderUnableToPerformQuestion.InsufficientTrainingAnswerId => NotPerformedReason.InsufficientTraining,
            ReasonProviderUnableToPerformQuestion.MemberPhysicallyUnableAnswerId => NotPerformedReason.MemberPhysicallyUnable,
            _ => throw new UnsupportedAnswerForQuestionException(_evaluationId, _formVersionId, questionId, q.AnswerId, q.AnswerValue, _providerId)
        };
    }
    private string GetReasonNotes()
    {
        if (_answers.TryGetValue(KedReasonNotPerformedNoteQuestion.QuestionId, out var kedReasonNotPerformedNoteQuestionAnswerModel))
        {
            return kedReasonNotPerformedNoteQuestionAnswerModel.AnswerValue.Length < Application.MaxNotPerformedReasonNotesLength ? kedReasonNotPerformedNoteQuestionAnswerModel.AnswerValue : kedReasonNotPerformedNoteQuestionAnswerModel.AnswerValue[..Application.MaxNotPerformedReasonNotesLength];
        }
        
        if (_answers.TryGetValue(ReasonNotPerformedNoteQuestion.QuestionId, out var reasonNotPerformedNoteQuestionAnswerModel))
        {
            return reasonNotPerformedNoteQuestionAnswerModel.AnswerValue.Length < Application.MaxNotPerformedReasonNotesLength ? reasonNotPerformedNoteQuestionAnswerModel.AnswerValue : reasonNotPerformedNoteQuestionAnswerModel.AnswerValue[..Application.MaxNotPerformedReasonNotesLength];
        }

        return string.Empty;
    }
    #endregion Not Performed

    #region Helper methods
    /// <summary>
    /// Gets the answer to a required question that we cannot move forward without having the answer
    /// </summary>
    /// <exception cref="RequiredEvaluationQuestionMissingException"></exception>
    private EvaluationAnswerModel GetRequired(int questionId)
    {
        if (!TryGetOptional(questionId, out var answerModel))
            throw new RequiredEvaluationQuestionMissingException(_evaluationId, _formVersionId, questionId);

        return answerModel;
    }

    /// <summary>
    /// Attempts to get the answer to a required question that we can gracefully continue if it was not answered
    /// </summary>
    private bool TryGetRequired(int questionId, string questionName, out EvaluationAnswerModel answerModel)
    {
        if (TryGetOptional(questionId, out answerModel))
            return true;

        _logger.LogWarning("For EvaluationId={EvaluationId}, required {QuestionName} question/answer is missing - QuestionId={QuestionId}",
            _evaluationId, questionName, questionId);

        return false;
    }

    private bool TryGetOptional(int questionId, out EvaluationAnswerModel answerModel)
    {
        return _answers.TryGetValue(questionId, out answerModel);
    }

    private static bool ValidateAlphaBarcode(string alphaBarcode)
    {
        return !string.IsNullOrEmpty(alphaBarcode) && Regex.IsMatch(alphaBarcode, "^(?:[A-Z]{6}?)?$", RegexOptions.IgnoreCase);
    }

    private static bool ValidateNumericBarcode(string numericBarcode)
    {
        return !string.IsNullOrEmpty(numericBarcode) && Regex.IsMatch(numericBarcode, "^LGC-(?:[0-9]{4}[-]?){3}?$", RegexOptions.IgnoreCase);
    }
    #endregion Helper methods
}