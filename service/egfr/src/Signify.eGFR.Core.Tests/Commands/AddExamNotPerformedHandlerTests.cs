using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NotPerformedReason = Signify.eGFR.Core.Models.NotPerformedReason;

namespace Signify.eGFR.Core.Tests.Commands;

public sealed class AddExamNotPerformedHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _fixture = new();
    private readonly IMapper _mapper = A.Fake<IMapper>();

    public void Dispose()
    {
        _fixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _fixture.DisposeAsync();
    }

    private AddExamNotPerformedHandler CreateSubject() =>
        new(A.Dummy<ILogger<AddExamNotPerformedHandler>>(), _fixture.SharedDbContext, _mapper);

    [Fact]
    private async Task Handle_WithRequest_InsertsToDb()
    {
        const int ExamId = 1;
        const string notes = null;
        var request = new AddExamNotPerformed(new Exam(), NotPerformedReason.NotInterested, notes);

        var dto = new ExamNotPerformed
        {
            ExamId = ExamId
        };

        A.CallTo(() => _mapper.Map<ExamNotPerformed>(A<Exam>._))
            .Returns(dto);
        A.CallTo(() => _mapper.Map(A<NotPerformedReason>._, A<Exam>._))
            .Returns(request.Exam);

        var subject = CreateSubject();

        var result = await subject.Handle(request, CancellationToken.None);

        Assert.Single(_fixture.SharedDbContext.ExamNotPerformeds);
        Assert.Equal(ExamId, _fixture.SharedDbContext.ExamNotPerformeds.First().ExamId);
        Assert.Equal(ExamId, result.ExamId);
    }
}