using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Tests.Utilities;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Commands;

public class CreateStatusHandlerTest : IClassFixture<EntityFixtures>, IClassFixture<MockDbFixture>
{
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly MockDbFixture _mockDbFixture;
    private readonly CreateStatusHandler _handler;

    public CreateStatusHandlerTest(MockDbFixture mockDbFixture)
    {
        var log = A.Fake<ILogger<CreateStatusHandler>>();
        _mockDbFixture = mockDbFixture;
        _handler = new CreateStatusHandler(log, _applicationTime, _mockDbFixture.FakeDatabaseContext);
    }

    [Fact]
    public async Task CreateStatusHandler_AddExistingStatus_ReturnFalseForIsNew()
    {
        // Arrange
        var request = new CreateStatus
        {
            ExamId = 1,
            ExamStatusCode = ExamStatusCode.Create(ExamStatusCode.ExamCreated.Name),
            MessageDateTime = _applicationTime.LocalNow()
        };

        _mockDbFixture.FakeDatabaseContext.ExamStatuses.Add(new ExamStatus
        {
            ExamId = 1,
            ExamStatusCodeId = 1,
            CreatedDateTime = _applicationTime.LocalNow()
        });
        await _mockDbFixture.FakeDatabaseContext.SaveChangesAsync();

        // Act
        var actual = await _handler.Handle(request, CancellationToken.None);

        // Assert
        actual.IsNew.Should().BeFalse();
        actual.ExamStatus.ExamStatusCodeId.Should().Be(ExamStatusCode.ExamCreated.ExamStatusCodeId);
    }

    [Fact]
    public async Task CreateStatusHandler_AddNewStatus_ReturnTrueForIsNew()
    {
        // Arrange
        var request = new CreateStatus
        {
            ExamId = 2,
            ExamStatusCode = ExamStatusCode.Create(ExamStatusCode.IRISAwaitingInterpretation.Name),
            MessageDateTime = _applicationTime.UtcNow()
        };

        // Act
        var actual = await _handler.Handle(request, CancellationToken.None);

        // Assert
        actual.IsNew.Should().BeTrue();
        actual.ExamStatus.ExamStatusCodeId.Should().Be(ExamStatusCode.IRISAwaitingInterpretation.ExamStatusCodeId);
    }
}