using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public sealed class AddExamHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private AddExamHandler CreateSubject() => new(A.Dummy<ILogger<AddExamHandler>>(), _dbFixture.SharedDbContext);

    [Fact]
    public async Task Handle_WithRequest_AddsExamToDatabase()
    {
        var exam = new SpirometryExam();

        var request = new AddExam(exam);

        var subject = CreateSubject();

        var actualResult = await subject.Handle(request, CancellationToken.None);

        Assert.Single(_dbFixture.SharedDbContext.SpirometryExams);
        Assert.Equal(exam, _dbFixture.SharedDbContext.SpirometryExams.First());
        Assert.Equal(exam, actualResult);
    }
}