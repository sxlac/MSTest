using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Infrastructure;
using Xunit;

namespace Signify.uACR.Core.Tests.Commands;

public sealed class AddLabResultHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();
    private readonly IApplicationTime _applicationTime = A.Fake<IApplicationTime>();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
        => _dbFixture.DisposeAsync();

    private AddLabResultHandler CreateSubject()
        => new(A.Dummy<ILogger<AddLabResultHandler>>(), _dbFixture.SharedDbContext);

    [Fact]
    public async Task Handle_WithRequest_AddsLabResultToDatabase()
    {
        //Arrange
        var labResult = new LabResult
        {
            UacrResult = (decimal)2.49,
            ResultColor = "Green",
            Normality = "Normal",
            CreatedDateTime = _applicationTime.UtcNow(),
            NormalityCode = "N"
        };
        var request = new AddLabResult(labResult);
        var subject = CreateSubject();
        
        //Act
        var actualResult = await subject.Handle(request, CancellationToken.None);
        
        //Assert
        Assert.Single(_dbFixture.SharedDbContext.LabResults);
        Assert.Equal(labResult, _dbFixture.SharedDbContext.LabResults.First());
        Assert.Equal(labResult, actualResult);
    }
}