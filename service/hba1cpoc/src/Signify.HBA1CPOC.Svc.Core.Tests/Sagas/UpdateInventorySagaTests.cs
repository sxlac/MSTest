using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.HBA1CPOC.Sagas;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Sagas.Commands;
using Signify.HBA1CPOC.Svc.Core.Sagas.Models;
using Signify.HBA1CPOC.Svc.Core.Sagas;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Sagas;

public class UpdateInventorySagaTests : IClassFixture<MockDbFixture>
{
    private readonly IMediator _mediator;
    private readonly IMessageHandlerContext _context;
    private readonly IInventoryApi _inventoryApi;
    private readonly UpdateInventorySaga _updateInventorySaga;

    public UpdateInventorySagaTests(MockDbFixture mockDbFixture)
    {
        _mediator = A.Fake<IMediator>();
        _inventoryApi = A.Fake<IInventoryApi>();
        _context = A.Fake<IMessageHandlerContext>();
        _updateInventorySaga = new UpdateInventorySaga(A.Dummy<ILogger<UpdateInventorySaga>>(), A.Dummy<IMapper>(), _mediator, _inventoryApi, mockDbFixture.Context)
            {
                Data = new UpdateInventorySagaData()
            };
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    public async Task Handle_InventoryUpdateReceived_Should_Mark_Completed(int ckdId, int evalId)
    {
        //Arrange
        var requestId = Guid.NewGuid();
        const int quantity = 1;
        const int providerId = 1;
        const string itemNumber = "000";
        var updateInventory = new UpdateInventory
        {
            CorrelationId = Guid.NewGuid(),
            HBA1CPOCId = ckdId,
            EvaluationId = evalId,
        };
        var apiResponse = new UpdateInventoryResponse
        {
            RequestId = requestId,
            Success = true
        };
        var result = new Result
            { IsSuccess = true };
        var inventoryUpdateReceived = new InventoryUpdateReceived(
            requestId, itemNumber, result, quantity, providerId, DateTime.Now, DateOnly.FromDateTime(DateTime.Now)
        );

        A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None))
            .Returns(EntityFixtures.MockCreateHBA1CPOCStatus());
        //Act
        await _updateInventorySaga.Handle(updateInventory, _context); //initiate
        await _updateInventorySaga.Handle(inventoryUpdateReceived, _context); //complete

        //Assert
        Assert.True(_updateInventorySaga.Completed);
    }
    [Fact]
    public async Task Handle_InventoryUpdateReceived_Data_Check()
    {
        //Arrange
        var requestId = Guid.NewGuid();
        const int quantity = 1;
        const int providerId = 1;
        const string itemNumber = "000";
        var UpdateInventory = new UpdateInventory
        {
            CorrelationId = Guid.NewGuid(),
            HBA1CPOCId = 1,
            EvaluationId = 1,
        };
        var apiResponse = new UpdateInventoryResponse
        {
            RequestId = requestId,
            Success = true
        };
        var result = new Result
            { IsSuccess = true };
        var inventoryUpdateReceived = new InventoryUpdateReceived(
            requestId, itemNumber, result, quantity, providerId, DateTime.Now, DateOnly.FromDateTime(DateTime.Now)
        );

        A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None))
            .Returns(EntityFixtures.MockCreateHBA1CPOCStatus());
        //Act
        await _updateInventorySaga.Handle(UpdateInventory, _context); //initiate
        await _updateInventorySaga.Handle(inventoryUpdateReceived, _context); //complete

        //Assert
        _updateInventorySaga.Data.HBA1CPOCId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_InventoryUpdateReceived_Data_TypeCheck()
    {
        //Arrange
        var requestId = Guid.NewGuid();
        const int quantity = 1;
        const int providerId = 1;
        const string itemNumber = "000";
        var updateInventory = new UpdateInventory
        {
            CorrelationId = Guid.NewGuid(),
            HBA1CPOCId = 1,
            EvaluationId = 1,
        };
        var apiResponse = new UpdateInventoryResponse
        {
            RequestId = requestId,
            Success = true
        };
        var result = new Result
            { IsSuccess = true };
        var inventoryUpdateReceived = new InventoryUpdateReceived(
            requestId, itemNumber, result, quantity, providerId, DateTime.Now, DateOnly.FromDateTime(DateTime.Now)
        );

        A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None))
            .Returns(EntityFixtures.MockCreateHBA1CPOCStatus());
        //Act
        await _updateInventorySaga.Handle(updateInventory, _context); //initiate
        await _updateInventorySaga.Handle(inventoryUpdateReceived, _context); //complete

        //Assert
        _updateInventorySaga.Data.Should().BeOfType<UpdateInventorySagaData>();
    }

    [Fact]
    public async Task Handle_InventoryUpdateReceived_When_InventoryApi_Return_Null()
    {
        var updateInventory = new UpdateInventory
        {
            CorrelationId = Guid.NewGuid(),
            HBA1CPOCId = 1,
            EvaluationId = 1
        };
        A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(new UpdateInventoryResponse());
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _updateInventorySaga.Handle(updateInventory, _context));
    }

    [Fact]
    public async Task Handle_InventoryUpdateReceived_Should_Call_InventoryApi_Once()
    {
        var updateInventory = new UpdateInventory
        {
            CorrelationId = Guid.NewGuid(),
            HBA1CPOCId = 1,
            EvaluationId = 1
        };
        A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(new UpdateInventoryResponse());
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _updateInventorySaga.Handle(updateInventory, _context));
        A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Handle_InventoryUpdateReceived_Should_Call_Mediator_Twice()
    {
        //Arrange
        var requestId = Guid.NewGuid();
        const int quantity = 1;
        const int providerId = 1;
        const string itemNumber = "000";
        var updateInventory = new UpdateInventory
        {
            CorrelationId = Guid.NewGuid(),
            HBA1CPOCId = 1,
            EvaluationId = 1,
        };
        var apiResponse = new UpdateInventoryResponse
        {
            RequestId = requestId,
            Success = true
        };
        var result = new Result
            { IsSuccess = true };
        var inventoryUpdateReceived = new InventoryUpdateReceived(
            requestId, itemNumber, result, quantity, providerId, DateTime.Now, DateOnly.FromDateTime(DateTime.Now)
        );

        A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None))
            .Returns(EntityFixtures.MockCreateHBA1CPOCStatus());
        //Act
        await _updateInventorySaga.Handle(updateInventory, _context); //initiate
        await _updateInventorySaga.Handle(inventoryUpdateReceived, _context); //complete

        //Assert
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None)).MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task Handle_UpdateInventory_Should_Call_Mediator_Once()
    {
        //Arrange
        var requestId = Guid.NewGuid();
        var updateInventory = new UpdateInventory
        {
            CorrelationId = Guid.NewGuid(),
            HBA1CPOCId = 1,
            EvaluationId = 1,
        };
        var apiResponse = new UpdateInventoryResponse
        {
            RequestId = requestId,
            Success = true
        };

        A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None))
            .Returns(EntityFixtures.MockCreateHBA1CPOCStatus());
        //Act
        await _updateInventorySaga.Handle(updateInventory, _context); //initiate

        //Assert
        A.CallTo(() => _mediator.Send(A<CreateHBA1CPOCStatus>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
    }
}