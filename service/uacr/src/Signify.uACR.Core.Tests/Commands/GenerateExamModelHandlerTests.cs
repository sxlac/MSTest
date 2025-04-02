using FakeItEasy;
using MediatR;
using Signify.uACR.Core.ApiClients.EvaluationApi.Responses;
using Signify.uACR.Core.Builders;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Models;
using Signify.uACR.Core.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.uACR.Core.Tests.Commands;

public class GenerateExamModelHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IExamModelBuilder _builder = A.Fake<IExamModelBuilder>();

    private GenerateExamModelHandler CreateSubject() => new(_mediator, _builder);

    [Fact]
    public async Task Handle_WithRequest_ReturnsBuiltExamModel()
    {
        const long evaluationId = 1;
        const int formVersionId = 900;
        const long providerId = 1;
        const string notes = null;
        var request = new GenerateExamModel(evaluationId);

        var evaluationModel = new EvaluationModel
        {
            FormVersionId = formVersionId,
            Answers = [],
            ProviderId = providerId
        };

        var expectedResult = new ExamModel(evaluationId, NotPerformedReason.NotInterested, notes);

        A.CallTo(() => _mediator.Send(A<QueryEvaluationModel>._, A<CancellationToken>._))
            .Returns(evaluationModel);
        A.CallTo(() => _builder.ForEvaluation(A<long>._))
            .Returns(_builder);
        A.CallTo(() => _builder.WithFormVersion(A<int>._))
            .Returns(_builder);
        A.CallTo(() => _builder.WithAnswers(A<IEnumerable<EvaluationAnswerModel>>._))
            .Returns(_builder);
        A.CallTo(() => _builder.WithProviderId(A<long>._))
            .Returns(_builder);
        A.CallTo(() => _builder.Build())
            .Returns(expectedResult);

        var subject = CreateSubject();

        var actualResult = await subject.Handle(request, CancellationToken.None);

        A.CallTo(() => _mediator.Send(A<QueryEvaluationModel>.That.Matches(q => q.EvaluationId == evaluationId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _builder.ForEvaluation(evaluationId))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _builder.WithFormVersion(formVersionId))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _builder.WithAnswers(evaluationModel.Answers))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _builder.WithProviderId(providerId))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _builder.Build())
            .MustHaveHappenedOnceExactly();

        Assert.Equal(expectedResult, actualResult);
    }
}