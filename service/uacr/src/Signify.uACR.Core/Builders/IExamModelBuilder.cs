using System;
using System.Collections.Generic;
using Signify.uACR.Core.ApiClients.EvaluationApi.Responses;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.Models;

namespace Signify.uACR.Core.Builders;

/// <summary>
/// Interface using the Builder pattern to create an <see cref="ExamModel"/> object
/// </summary>
public interface IExamModelBuilder
{
    /// <summary>
    /// Instructs this builder which evaluation this exam corresponds to
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <see cref="evaluationId"/> is not positive</exception>
    /// <returns></returns>
    public IExamModelBuilder ForEvaluation(long evaluationId);

    /// <summary>
    /// Instructs this builder which evaluation this exam corresponds to
    /// </summary>
    /// <param name="formVersionId"></param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <see cref="formVersionId"/> is not positive</exception>
    /// <returns></returns>
    public IExamModelBuilder WithFormVersion(int formVersionId);
    
    /// <summary>
    /// Instructs this builder to use these answers when building the results
    /// </summary>
    /// <param name="answers"></param>
    /// <exception cref="ArgumentNullException"></exception>
    IExamModelBuilder WithAnswers(IEnumerable<EvaluationAnswerModel> answers);
    
    /// <summary>
    /// Instructs this builder which evaluation this exam corresponds to
    /// </summary>
    /// <param name="providerId"></param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <see cref="providerId"/> is not positive</exception>
    /// <returns></returns>
    IExamModelBuilder WithProviderId(long providerId);

    /// <summary>
    /// Builds a new instance of a <see cref="ExamModel"/> from the previously-supplied answers
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this method is called before calling <see cref="ForEvaluation"/> or <see cref="WithAnswers"/>
    /// </exception>
    /// <exception cref="AnswerValueFormatException" />
    /// <exception cref="RequiredEvaluationQuestionMissingException" />
    /// <exception cref="UnsupportedAnswerForQuestionException" />
    /// <returns></returns>
    ExamModel Build();
}