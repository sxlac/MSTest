using AutoMapper;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.eGFR.Core.Tests.Commands;

public sealed class AddBarcodeTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();
    private readonly IMapper _mapper = A.Fake<IMapper>();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private AddBarcodeHandler CreateSubject() => new(A.Dummy<ILogger<AddBarcodeHandler>>(), _dbFixture.SharedDbContext);

    [Fact]
    public async Task Handle_WithRequest_AddsBarcodeToDatabase()
    {
        var exam = new Exam
        {
            ExamId = 1,
            ApplicationId = nameof(ApplicationId)
        };

        var rawResults = new RawExamResult
        {
            Barcode = "1010101",
            EvaluationId = 1
        };

        var request = new AddBarcode(exam, rawResults);

        var subject = CreateSubject();

        var actualResult = await subject.Handle(request, CancellationToken.None);

        Assert.Single(_dbFixture.SharedDbContext.BarcodeHistories);
        Assert.Equal(actualResult, _dbFixture.SharedDbContext.BarcodeHistories.First());
    }
}