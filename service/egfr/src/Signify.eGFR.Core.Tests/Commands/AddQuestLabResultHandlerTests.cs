using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signify.eGFR.Core.Infrastructure;
using Xunit;

namespace Signify.eGFR.Core.Tests.Commands;

public sealed class AddQuestLabResultHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();
    private readonly IApplicationTime _applicationTime = A.Fake<IApplicationTime>();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private AddQuestLabResultHandler CreateSubject()
        => new(A.Dummy<ILogger<AddQuestLabResultHandler>>(), _dbFixture.SharedDbContext);

    [Fact]
    public async Task Handle_WithRequest_AddsLabResultToDatabase()
    {
        //Arrange
        var labResult = new QuestLabResult
        {
            VendorLabTestId = 98765,
            VendorLabTestNumber = "K123456",
            eGFRResult = 63,
            CreatinineResult = (decimal)1.27,
            CenseoId = "X123456",
            Normality = "Normal",
            MailDate = _applicationTime.UtcNow(),
            AccessionedDate = _applicationTime.UtcNow(),
            CreatedDateTime = _applicationTime.UtcNow(),
            CollectionDate = _applicationTime.UtcNow(),
            NormalityCode = "N"
        };
        var request = new AddQuestLabResult(labResult);
        var subject = CreateSubject();
        
        
        //Act
        var actualResult = await subject.Handle(request, CancellationToken.None);
        
        //Assert
        Assert.Single(_dbFixture.SharedDbContext.QuestLabResults);
        Assert.Equal(labResult, _dbFixture.SharedDbContext.QuestLabResults.First());
        Assert.Equal(labResult, actualResult);
    }
}