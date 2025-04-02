using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Maps;
using Signify.CKD.Svc.Core.Messages.Status;
using Signify.CKD.Svc.Core.Tests.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Commands;

public sealed class AddExamNotPerformedHandlerTests : IAsyncDisposable, IDisposable
{
    private const int NotPerformedReasonId = 3;

    private readonly MockDbFixture _fixture = new MockDbFixture();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();

    public void Dispose()
    {
        _fixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _fixture.DisposeAsync();
    }

    private AddExamNotPerformedHandler CreateSubject()
        => new AddExamNotPerformedHandler(A.Dummy<ILogger<AddExamNotPerformedHandler>>(), _fixture.Context, _mapper, _mediator);

    private async Task AddNotPerformedReason()
    {
        if (await _fixture.Context.NotPerformedReason.FirstOrDefaultAsync(each =>
            each.NotPerformedReasonId == NotPerformedReasonId) != null)
        {
            return;
        }

        await _fixture.Context.NotPerformedReason.AddAsync(new NotPerformedReason
        {
            NotPerformedReasonId = 3,
            Reason = "Member apprehension",
            AnswerId = 30865
        });

        await _fixture.Context.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WhenNewRecord_InsertsRecord()
    {
        const int ckdId = 1;
        const string notes = "Member declined exam";

        var request = new AddExamNotPerformed(new Core.Data.Entities.CKD
        {
            CKDId = ckdId
        }, NotPerformedReasonId, notes);

        await AddNotPerformedReason();

        A.CallTo(() => _mapper.Map<ExamNotPerformed>(A<Core.Data.Entities.CKD>._))
            .Returns(new ExamNotPerformed
            {
                CKDId = ckdId
            });

        var existingCount = await _fixture.Context.ExamNotPerformed.CountAsync();

        var subject = CreateSubject();

        var actual = await subject.Handle(request, default);
        Assert.Equal(existingCount + 1, await _fixture.Context.ExamNotPerformed.CountAsync());

        A.CallTo(() => _mapper.Map<ExamNotPerformed>(A<Core.Data.Entities.CKD>.That.Matches(c =>
                c.CKDId == ckdId)))
            .MustHaveHappened();

        Assert.NotNull(actual);
        Assert.Equal(ckdId, actual.CKDId);
        Assert.Equal(NotPerformedReasonId, actual.NotPerformedReasonId);

        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>.That.Matches(p =>
                    p.Status is NotPerformed &&
                    ((NotPerformed)p.Status).ReasonNotes == notes &&
                    ((NotPerformed)p.Status).ReasonType == "Member Refused" &&
                    ((NotPerformed)p.Status).Reason == "Member apprehension"),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_WhenRecordAlreadyExists_ReturnsExisting()
    {
        const int ckdId = 1;
        const string notes = "Member declined exam";

        var request = new AddExamNotPerformed(new Core.Data.Entities.CKD
        {
            CKDId = ckdId
        }, NotPerformedReasonId, notes);

        await AddNotPerformedReason();

        await _fixture.Context.ExamNotPerformed.AddAsync(new ExamNotPerformed
        {
            CKDId = ckdId
        });

        await _fixture.Context.SaveChangesAsync();

        var existingCount = await _fixture.Context.ExamNotPerformed.CountAsync();

        var subject = CreateSubject();

        var actual = await subject.Handle(request, default);

        Assert.Equal(existingCount, await _fixture.Context.ExamNotPerformed.CountAsync());

        Assert.NotNull(actual);

        A.CallTo(() => _mapper.Map<ExamNotPerformed>(A<Core.Data.Entities.CKD>._))
            .MustNotHaveHappened();
    }

    /// <summary>
    /// Ensures if a new NotPerformedReason is added to the db, but no corresponding answer
    /// added to the <see cref="AnswerToQuestionMap"/>, an exception is thrown so it goes to
    /// the NSB error queue, informing us of a bug that needs to be addressed with a code change.
    /// Then once resolved, we can simply replay the event(s) from the NSB error queue and it will
    /// get handled successfully.
    /// </summary>
    [Fact]
    public async Task Handle_WhenNotPerformedAnswerIdInAnswerMap_Throws()
    {
        var entity = new NotPerformedReason
        {
            NotPerformedReasonId = 99, // doesn't matter, just can't have a conflict
            Reason = "Some new reason",
            AnswerId = 99 // doesn't matter, just can't be in the AnswerToQuestionMap
        };

        Assert.False(AnswerToQuestionMap.AnswerTypeMap.ContainsKey(entity.AnswerId), "This test data is not properly configured; change the AnswerId to something invalid");

        await _fixture.Context.NotPerformedReason.AddAsync(entity);
        await _fixture.Context.SaveChangesAsync();

        var request = new AddExamNotPerformed(new Core.Data.Entities.CKD(), entity.NotPerformedReasonId, default);

        var subject = CreateSubject();

        await Assert.ThrowsAnyAsync<Exception>(async () => await subject.Handle(request, default));

        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}