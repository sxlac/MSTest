using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NServiceBus;
using Signify.A1C.Svc.Core.ApiClient;
using Signify.A1C.Svc.Core.ApiClient.Requests;
using Signify.A1C.Svc.Core.ApiClient.Response;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Queries;
using Signify.A1C.Svc.Core.Sagas;
using Signify.A1C.Svc.Core.Sagas.Models;
using Signify.A1C.Svc.Core.Tests.Mocks.Json;
using Signify.A1C.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.Sagas
{
    public class UpdateInventorySagaTest : IClassFixture<EntityFixtures>, IClassFixture<MockA1CDBFixture>
    {
        private readonly IMessageHandlerContext _context;
        private readonly IInventoryApi _inventoryApi;
        private readonly IMediator _mediator;
        private readonly UpdateInventorySaga _updateInventorySaga;
        private readonly EntityFixtures _entityFixtures;

        public UpdateInventorySagaTest(EntityFixtures entityFixtures)
        {
            _context = A.Fake<IMessageHandlerContext>();
            _inventoryApi = A.Fake<IInventoryApi>();
            _mediator = A.Fake<IMediator>();
            var mapper = A.Fake<IMapper>();
            var logger = A.Fake<ILogger<UpdateInventorySaga>>();
            _entityFixtures = entityFixtures;
            _updateInventorySaga = new UpdateInventorySaga(logger, mapper, _mediator, _inventoryApi);
            _updateInventorySaga.Data = new UpdateInventorySagaData();
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(3, 1)]
        public async Task Handle_InventoryUpdateReceived_Should_Mark_Completed(int a1cId, int evalId)
        {
            //Arrange
            var requestId = new Guid();
            var quantity = 1;
            var providerId = 1;
            var itemNumber = "000";

            var request = JsonConvert.DeserializeObject<UpdateInventoryRequest>(APIRequestUpdateInventory.INVENTORY_REQUEST);
            Assert.NotNull(request);
            request.A1CId = a1cId;
            request.EvaluationId = evalId;

            var apiResponse = new UpdateInventoryResponse
            {
                RequestId = requestId,
                Success = true
            };
            var result = new Result
            {
                IsSuccess = true
            };
            var inventoryUpdateReceived = new InventoryUpdateReceived(
                requestId, itemNumber, result, "12345", quantity, providerId, DateTime.Now
            );

            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
            A.CallTo(() => _mediator.Send(A<CreateA1CStatus>._, default))
                .Returns(_entityFixtures.MockA1CStatus());
            A.CallTo(() => _mediator.Send(A<QueryA1CWithId>._, default))
                .Returns(new QueryA1CResponse
                {
                    A1C = new Core.Data.Entities.A1C
                    {
                        A1CId = a1cId
                    }
                });

            //Act
            await _updateInventorySaga.Handle(request, _context); //initiate
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
            UpdateInventoryRequest updateInventory = JsonConvert.DeserializeObject<UpdateInventoryRequest>(APIRequestUpdateInventory.INVENTORY_REQUEST);
            Assert.NotNull(updateInventory);
            updateInventory.A1CId = 1;
            updateInventory.EvaluationId = 1;
            UpdateInventoryResponse apiResponse = new UpdateInventoryResponse
            {
                RequestId = requestId,
                Success = true
            };
            Result result = new Result
            { IsSuccess = true };
            InventoryUpdateReceived inventoryUpdateReceived = new InventoryUpdateReceived(
                requestId, itemNumber, result, "1,2,3", quantity, providerId, DateTime.Now
            );

            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
            A.CallTo(() => _mediator.Send(A<CreateA1CStatus>._, CancellationToken.None))
                .Returns(_entityFixtures.MockA1CStatus());
            A.CallTo(() => _mediator.Send(A<QueryA1CWithId>._, CancellationToken.None))
                .Returns(_entityFixtures.MockQueryA1CResponse());
            //Act
            await _updateInventorySaga.Handle(updateInventory, _context); //initiate
            await _updateInventorySaga.Handle(inventoryUpdateReceived, _context); //complete

            //Assert
            _updateInventorySaga.Data.HBA1CId.Should().Be(1);
        }

        [Fact]
        public async Task Handle_InventoryUpdateReceived_Data_TypeCheck()
        {
            //Arrange
            var requestId = new Guid();
            var quantity = 1;
            var providerId = 1;
            var itemNumber = "000";
            UpdateInventoryRequest updateInventory = JsonConvert.DeserializeObject<UpdateInventoryRequest>(APIRequestUpdateInventory.INVENTORY_REQUEST);
            Assert.NotNull(updateInventory);
            updateInventory.A1CId = 1;
            updateInventory.EvaluationId = 1;
            UpdateInventoryResponse apiResponse = new UpdateInventoryResponse
            {
                RequestId = requestId,
                Success = true
            };
            Result result = new Result
            { IsSuccess = true };
            InventoryUpdateReceived inventoryUpdateReceived = new InventoryUpdateReceived(
                requestId, itemNumber, result, "", quantity, providerId, DateTime.Now
            );

            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
            A.CallTo(() => _mediator.Send(A<CreateA1CStatus>._, CancellationToken.None))
                .Returns(_entityFixtures.MockA1CStatus());
            A.CallTo(() => _mediator.Send(A<QueryA1CWithId>._, CancellationToken.None))
                .Returns(_entityFixtures.MockQueryA1CResponse());
            //Act
            await _updateInventorySaga.Handle(updateInventory, _context); //initiate
            await _updateInventorySaga.Handle(inventoryUpdateReceived, _context); //complete

            //Assert
            _updateInventorySaga.Data.Should().BeOfType<UpdateInventorySagaData>();
        }

        [Fact]
        public async Task Handle_InventoryUpdateReceived_When_InventoryApi_Return_Null()
        {
            UpdateInventoryRequest updateInventory = JsonConvert.DeserializeObject<UpdateInventoryRequest>(APIRequestUpdateInventory.INVENTORY_REQUEST);
            Assert.NotNull(updateInventory);
            updateInventory.A1CId = 1;
            updateInventory.EvaluationId = 1;
            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(new UpdateInventoryResponse());
            await Assert.ThrowsAsync<ApplicationException>(async () =>
                await _updateInventorySaga.Handle(updateInventory, _context));
        }

        [Fact]
        public async Task Handle_InventoryUpdateReceived_Should_Call_InventoryApi_Once()
        {
            UpdateInventoryRequest updateInventory = JsonConvert.DeserializeObject<UpdateInventoryRequest>(APIRequestUpdateInventory.INVENTORY_REQUEST);
            Assert.NotNull(updateInventory);
            updateInventory.A1CId = 1;
            updateInventory.EvaluationId = 1;
            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(new UpdateInventoryResponse());
            await Assert.ThrowsAsync<ApplicationException>(async () =>
                await _updateInventorySaga.Handle(updateInventory, _context));
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
            UpdateInventoryRequest updateInventory = JsonConvert.DeserializeObject<UpdateInventoryRequest>(APIRequestUpdateInventory.INVENTORY_REQUEST);
            Assert.NotNull(updateInventory);
            updateInventory.A1CId = 1;
            updateInventory.EvaluationId = 1;
            UpdateInventoryResponse apiResponse = new UpdateInventoryResponse
            {
                RequestId = requestId,
                Success = true
            };
            Result result = new Result
            { IsSuccess = true };
            InventoryUpdateReceived inventoryUpdateReceived = new InventoryUpdateReceived(
                requestId, itemNumber, result, "", quantity, providerId, DateTime.Now
            );

            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
            A.CallTo(() => _mediator.Send(A<CreateA1CStatus>._, CancellationToken.None))
                .Returns(_entityFixtures.MockA1CStatus());
            A.CallTo(() => _mediator.Send(A<QueryA1CWithId>._, CancellationToken.None))
                .Returns(_entityFixtures.MockQueryA1CResponse());
            //Act
            await _updateInventorySaga.Handle(updateInventory, _context); //initiate
            await _updateInventorySaga.Handle(inventoryUpdateReceived, _context); //complete

            //Assert
            A.CallTo(() => _mediator.Send(A<CreateA1CStatus>._, CancellationToken.None)).MustHaveHappenedTwiceExactly();
        }

        [Fact]
        public async Task Handle_UpdateInventory_Should_Call_Mediator_Once()
        {
            //Arrange
            var requestId = new Guid();
            UpdateInventoryRequest updateInventory = JsonConvert.DeserializeObject<UpdateInventoryRequest>(APIRequestUpdateInventory.INVENTORY_REQUEST);
            Assert.NotNull(updateInventory);
            updateInventory.A1CId = 1;
            updateInventory.EvaluationId = 1;
            UpdateInventoryResponse apiResponse = new UpdateInventoryResponse
            {
                RequestId = requestId,
                Success = true
            };

            A.CallTo(() => _inventoryApi.Inventory(A<UpdateInventoryRequest>._)).Returns(apiResponse);
            A.CallTo(() => _mediator.Send(A<CreateA1CStatus>._, CancellationToken.None))
                .Returns(_entityFixtures.MockA1CStatus());
            //Act
            await _updateInventorySaga.Handle(updateInventory, _context); //initiate

            //Assert
            A.CallTo(() => _mediator.Send(A<CreateA1CStatus>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }
    }
}
