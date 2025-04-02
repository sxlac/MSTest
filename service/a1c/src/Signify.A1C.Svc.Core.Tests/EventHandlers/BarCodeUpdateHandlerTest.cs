using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NServiceBus.Testing;
using Signify.A1C.Core.Events;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.ApiClient;
using Signify.A1C.Svc.Core.ApiClient.Requests;
using Signify.A1C.Svc.Core.ApiClient.Response;
using Signify.A1C.Svc.Core.EventHandlers;
using Signify.A1C.Svc.Core.Queries;
using Signify.A1C.Svc.Core.Tests.Mocks.Json.Queries;
using Signify.A1C.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.EventHandlers
{
   public class BarCodeUpdateHandlerTest : IClassFixture<EntityFixtures>, IClassFixture<MockA1CDBFixture>
    {
        private readonly ILogger<BarCodeUpdateHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IEvaluationApi _evaluationApi;
        private readonly IMapper _mapper;
        private readonly BarCodeUpdateHandler _barCodeUpdateHandler;
        private readonly TestableEndpointInstance _endpointInstance;
        private readonly EntityFixtures _entityFixtures;
        public BarCodeUpdateHandlerTest(EntityFixtures entityFixtures, MockA1CDBFixture mockDbFixture)
        {
            _endpointInstance = new TestableEndpointInstance();
            _logger = A.Fake<ILogger<BarCodeUpdateHandler>>();
            _mediator = A.Fake<IMediator>();
            _mapper = A.Fake<IMapper>();
            _evaluationApi = A.Fake<IEvaluationApi>();
            _entityFixtures = entityFixtures;
            _barCodeUpdateHandler =
                new BarCodeUpdateHandler(_logger, _mediator, _mapper, mockDbFixture.Context, _endpointInstance, _evaluationApi);
        }

        [Fact]
        public async Task BarCodeHandler_Should_Not_Publish_Message_For_Empty_EvalId()
        {
            BarcodeUpdate @event = new BarcodeUpdate()
                {Barcode = "12345", MemberPlanId = 21074285, ProductCode = "HBA1C"};
           await _barCodeUpdateHandler.Handle(@event, CancellationToken.None);
            _endpointInstance.PublishedMessages.Length.Should().Be(0);
        }

        [Fact]
        public async Task BarCodeHandler_Should_Not_Publish_Message_For_Diff_ProdcutCode()
        {
            BarcodeUpdate @event = new BarcodeUpdate()
                { Barcode = "12345", EvaluationId = 324357, MemberPlanId = 21074285, ProductCode = "HBA1CPOC" };
            await _barCodeUpdateHandler.Handle(@event, CancellationToken.None);
            _endpointInstance.PublishedMessages.Length.Should().Be(0);
        }

        [Fact]
        public async Task BarCodeHandler_Should_Publish_Message_For_New_EvalId()
        {
            BarcodeUpdate @event = new BarcodeUpdate()
                { Barcode = "12345", EvaluationId = 3243571, MemberPlanId = 21074285, ProductCode = "HBA1C" };
            A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponse());
            A.CallTo(() => _mediator.Send(A<GetA1C>._, CancellationToken.None)).Returns((Core.Data.Entities.A1C)null);
            A.CallTo(() => _mapper.Map( A<EvaluationModel>._, A<A1CEvaluationReceived>._)).Returns(_entityFixtures.MockEvalReceived());
            A.CallTo(() => _mapper.Map(@event, A<A1CEvaluationReceived>._)).Returns(_entityFixtures.MockEvalReceived());
            await _barCodeUpdateHandler.Handle(@event, CancellationToken.None);
            _endpointInstance.PublishedMessages.Length.Should().Be(1);
        }

        [Fact]
        public async Task BarCodeHandler_Should_Publish_Message_For_BarCode_Change()
        {
            BarcodeUpdate @event = new BarcodeUpdate()
                { Barcode = "12345", EvaluationId = 324357, MemberPlanId = 21074285, ProductCode = "HBA1C" };
            A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponse());
            var inventory = new UpdateInventoryRequest() { };
            A.CallTo(() => _mapper.Map(A<Core.Data.Entities.A1C>._, A<UpdateInventoryRequest>._)).Returns(inventory);
            await _barCodeUpdateHandler.Handle(@event, CancellationToken.None);
            _endpointInstance.SentMessages.Length.Should().Be(1);
        }


        [Fact]
        public async Task BarCodeHandler_Should_Not_Publish_Message_No_BarCode_Change()
        {
            BarcodeUpdate @event = new BarcodeUpdate()
                { Barcode = "", EvaluationId = 324357, MemberPlanId = 21074285, ProductCode = "HBA1C" };
            A.CallTo(() => _evaluationApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponse());
            await _barCodeUpdateHandler.Handle(@event, CancellationToken.None);
            _endpointInstance.PublishedMessages.Length.Should().Be(0);
        }

        private static EvaluationVersionRs GetApiResponse()
        {
            return JsonConvert.DeserializeObject<EvaluationVersionRs>(QueriesAPIResponse.EVALUATIONVERSION);
        }
    }
}
