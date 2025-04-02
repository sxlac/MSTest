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

namespace Signify.Spirometry.Core.Tests.Commands;

public sealed class AddExamResultsHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly IMapper _mapper = A.Fake<IMapper>();

    private readonly MockDbFixture _dbFixture = new();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private AddExamResultsHandler CreateSubject()
        => new(A.Dummy<ILogger<AddExamResultsHandler>>(), _mapper, _dbFixture.SharedDbContext);

    [Fact]
    public async Task Handle_WithRequest_AddsExamResultsToDatabase()
    {
        const int spirometryExamId = 1;

        var request = new AddExamResults(spirometryExamId, new ExamResult());

        var expectedSpirometryExamResult = new SpirometryExamResult();

        A.CallTo(() => _mapper.Map<SpirometryExamResult>(A<ExamResult>._))
            .Returns(expectedSpirometryExamResult);

        var subject = CreateSubject();

        var actualResult = await subject.Handle(request, CancellationToken.None);

        A.CallTo(() => _mapper.Map<SpirometryExamResult>(A<ExamResult>._))
            .MustHaveHappenedOnceExactly();

        Assert.Single(_dbFixture.SharedDbContext.SpirometryExamResults);

        var actualEntityRecord = _dbFixture.SharedDbContext.SpirometryExamResults.First();
        Assert.Equal(actualEntityRecord, actualResult);
        Assert.Equal(spirometryExamId, actualResult.SpirometryExamId);
    }
}