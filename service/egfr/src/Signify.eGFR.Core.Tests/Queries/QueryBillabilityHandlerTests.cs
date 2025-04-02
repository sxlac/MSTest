using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Queries;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signify.eGFR.Core.Constants;
using Xunit;

namespace Signify.eGFR.Core.Tests.Queries;

public class QueryBillabilityHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();

    private QueryBillabilityHandler CreateSubject() => new(A.Dummy<ILogger<QueryBillabilityHandler>>(), _mediator);

    [Fact]
    public async Task Handle_WithNoLabResults_NorNotPerformedReason_ReturnsFalse()
    {
        const int evaluationId = 100;

        A.CallTo(() => _mediator.Send(A<QueryQuestLabResultByEvaluationId>._, A<CancellationToken>._))
            .Returns((QuestLabResult)null);

        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns((ExamNotPerformed)null);

        var subject = CreateSubject();

        var result = await subject.Handle(new QueryBillability(Guid.NewGuid(), evaluationId), default);

        Assert.False(result.IsBillable);
    }

    [Theory]
    [MemberData(nameof(Handle_WithLabResults_WithNormality_IsBillable_TestData))]
    public async Task Handle_WithLabResults_WhenLabResultsSupplied_WithNormality_IsBillable_Tests(string indicator, bool expectedIsBillable)
    {
        const int evaluationId = 1;
        const string censeoId = "censeoId";
        DateTimeOffset? collectionDate = DateTime.UtcNow;

        var labResult = new QuestLabResult();
        if (indicator != null)
        {
            labResult.NormalityCode = indicator;
            labResult.CenseoId = censeoId;
            labResult.CollectionDate = collectionDate;
        }

        A.CallTo(() => _mediator.Send(A<QueryQuestLabResultByEvaluationId>._, A<CancellationToken>._))
            .Returns(labResult);

        var subject = CreateSubject();

        var result = await subject.Handle(new QueryBillability(Guid.NewGuid(), evaluationId, labResult), default);

        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        Assert.Equal(expectedIsBillable, result.IsBillable);
    }

    [Theory]
    [MemberData(nameof(Handle_WithLabResults_AllNormality_IsBillable_TestData))]
    public async Task Handle_WithLabResults_WhenLabResultsNotSupplied_WithNormality_IsBillable_Tests(string indicator, bool expectedIsBillable)
    {
        const int evaluationId = 1;
        const string censeoId = "censeoId";
        DateTimeOffset? collectionDate = DateTime.UtcNow;
        
        var labResult = new QuestLabResult();
        if (indicator != null)
        {
            labResult.NormalityCode = indicator;
            labResult.CenseoId = censeoId;
            labResult.CollectionDate = collectionDate;
        }

        var subject = CreateSubject();
        var result = await subject.Handle(new QueryBillability(Guid.NewGuid(), evaluationId, labResult), default);

        A.CallTo(() => _mediator.Send(A<QueryLabResultByEvaluationId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        Assert.Equal(expectedIsBillable, result.IsBillable);
    }

    public static IEnumerable<object[]> Handle_WithLabResults_WithNormality_IsBillable_TestData()
    {
        yield return [NormalityCodes.Normal, true];
        yield return [NormalityCodes.Abnormal, true];
    }

    public static IEnumerable<object[]> Handle_WithLabResults_AllNormality_IsBillable_TestData()
    {
        yield return [NormalityCodes.Normal, true];
        yield return [NormalityCodes.Abnormal, true];
        yield return [NormalityCodes.Undetermined, false];
    }

    [Fact]
    public async Task Handle_WhenNotPerformed_ReturnsFalse()
    {
        const int evaluationId = 100;

        A.CallTo(() => _mediator.Send(A<QueryQuestLabResultByEvaluationId>._, A<CancellationToken>._))
            .Returns((QuestLabResult)null);
        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns(new ExamNotPerformed());

        var subject = CreateSubject();
        var result = await subject.Handle(new QueryBillability(Guid.NewGuid(), evaluationId), default);

        Assert.False(result.IsBillable);
    }
}