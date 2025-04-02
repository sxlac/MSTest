using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Commands;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Commands;

public class CreateRcmBillingHandlerTests : IClassFixture<MockDbFixture>
{
    private readonly MockDbFixture _mockDbFixture;
    private readonly CreateRcmBillingHandler _createRcmBillingHandler;
    private readonly FakeApplicationTime _applicationTime = new();

    public CreateRcmBillingHandlerTests(MockDbFixture mockDbFixture)
    {
        _mockDbFixture = mockDbFixture;
        _createRcmBillingHandler = new CreateRcmBillingHandler(_mockDbFixture.FakeDatabaseContext);
    }

    [Fact]
    public async Task Should_Insert_RCMBilling_Status()
    {
        var rcmBilling = new CreateRcmBilling
        {
            RcmBilling = new DEEBilling
            {
                BillId = "1",
                ExamId = 1,
                CreatedDateTime = _applicationTime.UtcNow()
            }
        };

        //Act
        var result = await _createRcmBillingHandler.Handle(rcmBilling, CancellationToken.None);

        //Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Insert_RCMBilling_Count()
    {
        //Arrange
        var rcmBilling = new CreateRcmBilling
        {
            RcmBilling = new DEEBilling
            {
                BillId = "1",
                ExamId = 1,
                CreatedDateTime = _applicationTime.UtcNow()
            }
        };
        var initialCount = _mockDbFixture.FakeDatabaseContext.DEEBilling.Count();

        //Act
        await _createRcmBillingHandler.Handle(rcmBilling, CancellationToken.None);

        //Assert
        _mockDbFixture.FakeDatabaseContext.DEEBilling.Count().Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public async Task Should_Update_RCMBilling_When_Entity_AlreadyExist()
    {
        //Arrange
        var rcmBilling = new CreateRcmBilling
        {
            RcmBilling = new DEEBilling
            {
                BillId = "1",
                ExamId = 1,
                CreatedDateTime = _applicationTime.UtcNow()
                // this adds a new entry with Id = 2 since Id = 1 exist in fixture
            }
        };

        //Act
        await _createRcmBillingHandler.Handle(rcmBilling, CancellationToken.None);
        var initialCount = _mockDbFixture.FakeDatabaseContext.DEEBilling.Count();
        rcmBilling.RcmBilling.Accepted = true;
        rcmBilling.RcmBilling.AcceptedAt = _applicationTime.UtcNow();
        await _createRcmBillingHandler.Handle(rcmBilling, CancellationToken.None);

        //Assert
        Assert.Equal(initialCount, _mockDbFixture.FakeDatabaseContext.DEEBilling.Count());
    }
}