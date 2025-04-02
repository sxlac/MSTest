using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.eGFR.Core.Tests.Commands;

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
        var exam = new Exam
        {
            ApplicationId = nameof(ApplicationId)
        };

        var request = new AddExam(exam);

        var subject = CreateSubject();

        var actualResult = await subject.Handle(request, CancellationToken.None);

        Assert.Single(_dbFixture.SharedDbContext.Exams);
        Assert.Equal(exam, _dbFixture.SharedDbContext.Exams.First());
        Assert.Equal(exam, actualResult);
    }
}