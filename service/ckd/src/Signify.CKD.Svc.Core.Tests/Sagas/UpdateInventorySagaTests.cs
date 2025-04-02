using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.CKD.Sagas;
using Signify.CKD.Svc.Core.ApiClient;
using Signify.CKD.Svc.Core.ApiClient.Requests;
using Signify.CKD.Svc.Core.ApiClient.Response;
using Signify.CKD.Svc.Core.Sagas;
using Signify.CKD.Svc.Core.Sagas.Commands;
using Signify.CKD.Svc.Core.Sagas.Models;
using Signify.CKD.Svc.Core.Tests.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Signify.CKD.Svc.Core.Commands;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Sagas
{
    public class UpdateInventorySagaTests : IClassFixture<EntityFixtures>, IClassFixture<MockDbFixture>
    {
        private readonly IMediator _mediator;
        private readonly IMessageHandlerContext _context;
        private readonly IInventoryApi _inventoryApi;
        private readonly UpdateInventorySaga _updateInventorySaga;
        private readonly EntityFixtures _entityFixtures;

        public UpdateInventorySagaTests(EntityFixtures entityFixtures, MockDbFixture mockDbFixture)
        {
            var logger = A.Fake<ILogger<UpdateInventorySaga>>();
            _mediator = A.Fake<IMediator>();
            var mapper = A.Fake<IMapper>();
            _entityFixtures = entityFixtures;
            _inventoryApi = A.Fake<IInventoryApi>();
            _context = A.Fake<IMessageHandlerContext>();
            _updateInventorySaga = new UpdateInventorySaga(logger, mapper, _mediator, _inventoryApi, mockDbFixture.Context);
            _updateInventorySaga.Data = new UpdateInventorySagaData();
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(3, 1)]
        public async Task Handle_InventoryUpdateReceived_Should_Mark_Completed(int ckdId, int evalId)
        {
            //Arrange
            var requestId = new Guid();
            var quantity = 1;
            var providerId = 1;
            var itemNumber = "000";
            UpdateInventory UpdateInventory = new UpdateInventory
            {
                CorrelationId = new Guid(),
                CKDId = ckdId,
                EvaluationId = evalId,
            };
            UpdateInventoryResponse apiResponse = new UpdateInventoryResponse
            {
                RequestId = requestId,
                Success = true
            };
            Signify.CKD.Sagas.Result result = new Signify.CKD.Sagas.Result
            { IsSuccess = true };
            InventoryUpdateReceived inventoryUpdateReceived = new InventoryUpdateReceived(
                requestId, itemNumber, result, quantity, providerId, DateTime.Now, DateTime.Now
            );

            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
            A.CallTo(() => _mediator.Send(A<CreateCKDStatus>._, CancellationToken.None))
                .Returns(_entityFixtures.MockCKDStatus());
            //Act
            await _updateInventorySaga.Handle(UpdateInventory, _context); //initiate
            await _updateInventorySaga.Handle(inventoryUpdateReceived, _context); //complete

            //Assert
            Assert.True(_updateInventorySaga.Completed);
        }
        [Fact]
        public async Task Handle_InventoryUpdateReceived_Data_Check()
        {
            //Arrange
            var requestId = new Guid();
            var quantity = 1;
            var providerId = 1;
            var itemNumber = "000";
            UpdateInventory UpdateInventory = new UpdateInventory
            {
                CorrelationId = new Guid(),
                CKDId = 1,
                EvaluationId = 1,
            };
            UpdateInventoryResponse apiResponse = new UpdateInventoryResponse
            {
                RequestId = requestId,
                Success = true
            };
            Signify.CKD.Sagas.Result result = new Signify.CKD.Sagas.Result
            { IsSuccess = true };
            InventoryUpdateReceived inventoryUpdateReceived = new InventoryUpdateReceived(
                requestId, itemNumber, result, quantity, providerId, DateTime.Now, DateTime.Now
            );

            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
            A.CallTo(() => _mediator.Send(A<CreateCKDStatus>._, CancellationToken.None))
                .Returns(_entityFixtures.MockCKDStatus());
            //Act
            await _updateInventorySaga.Handle(UpdateInventory, _context); //initiate
            await _updateInventorySaga.Handle(inventoryUpdateReceived, _context); //complete

            //Assert
            _updateInventorySaga.Data.CKDId.Should().Be(1);
        }
        [Fact]
        public async Task Handle_InventoryUpdateReceived_Data_TypeCheck()
        {
            //Arrange
            var requestId = new Guid();
            var quantity = 1;
            var providerId = 1;
            var itemNumber = "000";
            UpdateInventory UpdateInventory = new UpdateInventory
            {
                CorrelationId = new Guid(),
                CKDId = 1,
                EvaluationId = 1,
            };
            UpdateInventoryResponse apiResponse = new UpdateInventoryResponse
            {
                RequestId = requestId,
                Success = true
            };
            Signify.CKD.Sagas.Result result = new Signify.CKD.Sagas.Result
            { IsSuccess = true };
            InventoryUpdateReceived inventoryUpdateReceived = new InventoryUpdateReceived(
                requestId, itemNumber, result, quantity, providerId, DateTime.Now, DateTime.Now
            );

            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
            A.CallTo(() => _mediator.Send(A<CreateCKDStatus>._, CancellationToken.None))
                .Returns(_entityFixtures.MockCKDStatus());
            //Act
            await _updateInventorySaga.Handle(UpdateInventory, _context); //initiate
            await _updateInventorySaga.Handle(inventoryUpdateReceived, _context); //complete

            //Assert
            _updateInventorySaga.Data.Should().BeOfType<UpdateInventorySagaData>();
        }
        [Fact]
        public async Task Handle_InventoryUpdateReceived_When_InventoryApi_Return_Null()
        {
            UpdateInventory UpdateInventory = new UpdateInventory
            {
                CorrelationId = new Guid(),
                CKDId = 1,
                EvaluationId = 1,
            };
            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(new UpdateInventoryResponse());
            await Assert.ThrowsAsync<ApplicationException>(async () =>
                await _updateInventorySaga.Handle(UpdateInventory, _context));
        }
        [Fact]
        public async Task Handle_InventoryUpdateReceived_Should_Call_InventoryApi_Once()
        {
            UpdateInventory UpdateInventory = new UpdateInventory
            {
                CorrelationId = new Guid(),
                CKDId = 1,
                EvaluationId = 1,
            };
            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(new UpdateInventoryResponse());
            await Assert.ThrowsAsync<ApplicationException>(async () =>
                await _updateInventorySaga.Handle(UpdateInventory, _context));
            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).MustHaveHappenedOnceExactly();
        }
        [Fact]
        public async Task Handle_InventoryUpdateReceived_Should_Call_Mediator_Twice()
        {
            //Arrange
            var requestId = new Guid();
            var quantity = 1;
            var providerId = 1;
            var itemNumber = "000";
            UpdateInventory UpdateInventory = new UpdateInventory
            {
                CorrelationId = new Guid(),
                CKDId = 1,
                EvaluationId = 1,
            };
            UpdateInventoryResponse apiResponse = new UpdateInventoryResponse
            {
                RequestId = requestId,
                Success = true
            };
            Result result = new Signify.CKD.Sagas.Result
            { IsSuccess = true };
            InventoryUpdateReceived inventoryUpdateReceived = new InventoryUpdateReceived(
                requestId, itemNumber, result, quantity, providerId, DateTime.Now, DateTime.Now
            );

            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
            A.CallTo(() => _mediator.Send(A<CreateCKDStatus>._, CancellationToken.None))
                .Returns(_entityFixtures.MockCKDStatus());
            //Act
            await _updateInventorySaga.Handle(UpdateInventory, _context); //initiate
            await _updateInventorySaga.Handle(inventoryUpdateReceived, _context); //complete

            //Assert
            A.CallTo(() => _mediator.Send(A<CreateCKDStatus>._, CancellationToken.None)).MustHaveHappenedTwiceExactly();
        }
        [Fact]
        public async Task Handle_UpdateInventory_Should_Call_Mediator_Once()
        {
            //Arrange
            var requestId = new Guid();
            var quantity = 1;
            var providerId = 1;
            var itemNumber = "000";
            UpdateInventory UpdateInventory = new UpdateInventory
            {
                CorrelationId = new Guid(),
                CKDId = 1,
                EvaluationId = 1,
            };
            UpdateInventoryResponse apiResponse = new UpdateInventoryResponse
            {
                RequestId = requestId,
                Success = true
            };
            Result result = new Signify.CKD.Sagas.Result
            { IsSuccess = true };
            InventoryUpdateReceived inventoryUpdateReceived = new InventoryUpdateReceived(
                requestId, itemNumber, result, quantity, providerId, DateTime.Now, DateTime.Now
            );

            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
            A.CallTo(() => _mediator.Send(A<CreateCKDStatus>._, CancellationToken.None))
                .Returns(_entityFixtures.MockCKDStatus());
            //Act
            await _updateInventorySaga.Handle(UpdateInventory, _context); //initiate

            //Assert
            A.CallTo(() => _mediator.Send(A<CreateCKDStatus>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

    }
}
