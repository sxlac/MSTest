using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;
using NotPerformedReason = Signify.Spirometry.Core.Models.NotPerformedReason;

namespace Signify.Spirometry.Core.Tests.Commands;

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
        const int spirometryExamId = 1;

        var request = new AddExamNotPerformed(new SpirometryExam(), new NotPerformedInfo(NotPerformedReason.NotInterested));

        var dto = new ExamNotPerformed
        {
            SpirometryExamId = spirometryExamId
        };

        A.CallTo(() => _mapper.Map<ExamNotPerformed>(A<SpirometryExam>._))
            .Returns(dto);
        A.CallTo(() => _mapper.Map(A<NotPerformedReason>._, A<SpirometryExam>._))
            .Returns(request.SpirometryExam);

        var subject = CreateSubject();

        var result = await subject.Handle(request, CancellationToken.None);

        Assert.Single(_fixture.SharedDbContext.ExamNotPerformeds);
        Assert.Equal(spirometryExamId, _fixture.SharedDbContext.ExamNotPerformeds.First().SpirometryExamId);
        Assert.Equal(spirometryExamId, result.SpirometryExamId);
    }
}