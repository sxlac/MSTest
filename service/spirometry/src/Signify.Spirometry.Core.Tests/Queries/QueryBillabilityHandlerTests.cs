using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryBillabilityHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();

    private QueryBillabilityHandler CreateSubject() => new(A.Dummy<ILogger<QueryBillabilityHandler>>(), _mediator);

    [Fact]
    public async Task Handle_WithNoExamResults_NorNotPerformedReason_Throws()
    {
        const int evaluationId = 1;

        A.CallTo(() => _mediator.Send(A<QueryExamResults>._, A<CancellationToken>._))
            .Returns((SpirometryExamResult)null);

        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns((ExamNotPerformed) null);

        var subject = CreateSubject();

        await Assert.ThrowsAnyAsync<UnableToDetermineBillabilityException>(async () =>
            await subject.Handle(new QueryBillability(Guid.NewGuid(), 1), default));

        A.CallTo(() => _mediator.Send(A<QueryExamResults>.That.Matches(q => q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>.That.Matches(q => q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Theory]
    [MemberData(nameof(Handle_WithExamResults_WithNormality_IsBillable_TestData))]
    public async Task Handle_WithExamResults_WhenExamResultsSupplied_WithNormality_IsBillable_Tests(NormalityIndicator indicator, bool expectedIsBillable)
    {
        const int evaluationId = 1;

        var queryResult = new SpirometryExamResult();
        if (indicator != null)
            queryResult.NormalityIndicatorId = indicator.NormalityIndicatorId;

        A.CallTo(() => _mediator.Send(A<QueryExamResults>._, A<CancellationToken>._))
            .Returns(queryResult);

        var subject = CreateSubject();

        var result = await subject.Handle(new QueryBillability(Guid.NewGuid(), evaluationId), default);

        A.CallTo(() => _mediator.Send(A<QueryExamResults>.That.Matches(q => q.EvaluationId == evaluationId), A<CancellationToken>._))
            .MustHaveHappened();
        Assert.Equal(expectedIsBillable, result.IsBillable);
    }

    [Theory]
    [MemberData(nameof(Handle_WithExamResults_WithNormality_IsBillable_TestData))]
    public async Task Handle_WithExamResults_WhenExamResultsNotSupplied_WithNormality_IsBillable_Tests(NormalityIndicator indicator, bool expectedIsBillable)
    {
        const int evaluationId = 1;

        var queryResult = new SpirometryExamResult();
        if (indicator != null)
            queryResult.NormalityIndicatorId = indicator.NormalityIndicatorId;

        var subject = CreateSubject();

        var result = await subject.Handle(new QueryBillability(Guid.NewGuid(), evaluationId, queryResult), default);

        A.CallTo(() => _mediator.Send(A<QueryExamResults>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        Assert.Equal(expectedIsBillable, result.IsBillable);
    }

    public static IEnumerable<object[]> Handle_WithExamResults_WithNormality_IsBillable_TestData()
    {
        yield return [NormalityIndicator.Normal, true];
        yield return [NormalityIndicator.Abnormal, true];
        yield return [NormalityIndicator.Undetermined, false];
        yield return [null, false]; // Not possible because it is not null and has a FK constraint, but covering anyways
    }

    [Fact]
    public async Task Handle_WhenNotPerformed_ReturnsFalse()
    {
        const int evaluationId = 1;

        A.CallTo(() => _mediator.Send(A<QueryExamResults>._, A<CancellationToken>._))
            .Returns((SpirometryExamResult) null);

        A.CallTo(() => _mediator.Send(A<QueryExamNotPerformed>._, A<CancellationToken>._))
            .Returns(new ExamNotPerformed());

        var subject = CreateSubject();

        var result = await subject.Handle(new QueryBillability(Guid.NewGuid(), evaluationId), default);

        Assert.False(result.IsBillable);
    }
}