using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.EventHandlers;
using Signify.A1C.Svc.Core.Events;
using Signify.A1C.Svc.Core.Queries;
using Signify.A1C.Svc.Core.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.EventHandlers
{
    public class EvaluationFinalizedHandlerTest : IClassFixture<EntityFixtures>, IClassFixture<MockA1CDBFixture>
    {
        private readonly ILogger<EvaluationFinalizedHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly EvaluationFinalizedHandler _evaluationFinalizedHandler;
        private readonly TestableEndpointInstance _endpointInstance;
        private readonly EntityFixtures _entityFixtures;
        public EvaluationFinalizedHandlerTest(EntityFixtures entityFixtures, MockA1CDBFixture mockDbFixture)
        {
            _endpointInstance = new TestableEndpointInstance();
            _logger = A.Fake<ILogger<EvaluationFinalizedHandler>>();
            _mediator = A.Fake<IMediator>();
            _mapper = A.Fake<IMapper>();
            _entityFixtures = entityFixtures;
            _evaluationFinalizedHandler =
                new EvaluationFinalizedHandler(_logger, _mediator, _endpointInstance, _mapper);
        }

        [Fact]
        public async Task EvaluationFinalizedHandler_WhenProductCodeIsNotA1C()
        {
            EvaluationFinalizedEvent @event = new EvaluationFinalizedEvent()
            {
                ApplicationId = "Signify.Evaluation.Service",
                AppointmentId = 1000084716,
                ClientId = 14,
                CreatedDateTime = DateTimeOffset.UtcNow,
                DateOfService = DateTime.UtcNow,
                DocumentPath = null,
                EvaluationId = 324357,
                EvaluationTypeId = 1,
                FormVersionId = 0,
                Location = new Location(32.925496267, 32.925496267),
                MemberId = 11990396,
                MemberPlanId = 21074285,
                ProviderId = 42879,
                ReceivedDateTime = DateTime.UtcNow,
                UserName = "vastest1",
                Products = new List<Product>() { new Product("HHRA"), new Product("A1C") }
            };
            await _evaluationFinalizedHandler.Handle(@event, CancellationToken.None);
            _endpointInstance.SentMessages.Length.Should().Be(0);
        }

        [Fact]
        public async Task EvaluationFinalizedHandler_WhenEvaluationIsAlreadyFinalized()
        {
            EvaluationFinalizedEvent @event = new EvaluationFinalizedEvent()
            {
                ApplicationId = "Signify.Evaluation.Service",
                AppointmentId = 1000084716,
                ClientId = 14,
                CreatedDateTime = DateTimeOffset.UtcNow,
                DateOfService = DateTime.UtcNow,
                DocumentPath = null,
                EvaluationId = 324357,
                EvaluationTypeId = 1,
                FormVersionId = 0,
                Location = new Location(32.925496267, 32.925496267),
                MemberId = 11990396,
                MemberPlanId = 21074285,
                ProviderId = 42879,
                ReceivedDateTime = DateTime.UtcNow,
                UserName = "vastest1",
                Products = new List<Product>() { new Product("HHRA"), new Product("A1C") }
            };
            await _evaluationFinalizedHandler.Handle(@event, CancellationToken.None);
            _endpointInstance.SentMessages.Length.Should().Be(0);
        }

        [Fact]
        public async Task Should_Not_Publish_WhenBarcode_Empty()
        {
            EvaluationFinalizedEvent @event = new EvaluationFinalizedEvent()
            {
                ApplicationId = "Signify.Evaluation.Service",
                AppointmentId = 1000084716,
                ClientId = 14,
                CreatedDateTime = DateTimeOffset.UtcNow,
                DateOfService = new DateTime(2021, 10, 10),
                DocumentPath = null,
                EvaluationId = 324359,
                EvaluationTypeId = 1,
                FormVersionId = 0,
                Location = new Location(32.925496267, 32.925496267),
                MemberId = 11990396,
                MemberPlanId = 21074285,
                ProviderId = 42879,
                ReceivedDateTime = DateTime.UtcNow,
                UserName = "vastest1",
                Products = new List<Product>() { new Product("HHRA"), new Product("A1C") }
            };
            A.CallTo(() => _mediator.Send(A<CheckA1CEval>._, CancellationToken.None)).Returns(string.Empty);
            await _evaluationFinalizedHandler.Handle(@event, CancellationToken.None);
            _endpointInstance.SentMessages.Length.Should().Be(0);
        }
        [Fact]
        public async Task Should_Publish_OnSuccess()
        {
            var barcode = "12345";
            Core.Data.Entities.A1C a1c = null;
            A.CallTo(() => _mediator.Send(A<CheckA1CEval>._, CancellationToken.None)).Returns(barcode);
            A.CallTo(() => _mediator.Send(A<GetA1C>._, CancellationToken.None)).Returns(a1c);
            A.CallTo(() => _mapper.Map<A1CEvaluationReceived>(A<EvaluationFinalizedEvent>._)).Returns(_entityFixtures.MockEvalReceived());
            await _evaluationFinalizedHandler.Handle(@event, CancellationToken.None);
            _endpointInstance.PublishedMessages.Length.Should().Be(1);
        }

        [Fact]
        public async Task Should_Publish_Check_Type()
        {
            var barcode = "12345";
            Core.Data.Entities.A1C a1c = null;
            A.CallTo(() => _mediator.Send(A<CheckA1CEval>._, CancellationToken.None)).Returns(barcode);
            A.CallTo(() => _mediator.Send(A<GetA1C>._, CancellationToken.None)).Returns(a1c);
            A.CallTo(() => _mapper.Map<A1CEvaluationReceived>(A<EvaluationFinalizedEvent>._)).Returns(_entityFixtures.MockEvalReceived());
            await _evaluationFinalizedHandler.Handle(@event, CancellationToken.None);
            _endpointInstance.PublishedMessages.Length.Should().Be(1);
            _endpointInstance.PublishedMessages[0].Message.Should().BeOfType<A1CEvaluationReceived>();
        }

        [Fact]
        public async Task Should_Publish_Check_Numbers()
        {
            var barcode = "12345";
            Core.Data.Entities.A1C a1c = null;
            A.CallTo(() => _mediator.Send(A<CheckA1CEval>._, CancellationToken.None)).Returns(barcode);
            A.CallTo(() => _mediator.Send(A<GetA1C>._, CancellationToken.None)).Returns(a1c);
            A.CallTo(() => _mapper.Map<A1CEvaluationReceived>(A<EvaluationFinalizedEvent>._)).Returns(_entityFixtures.MockEvalReceived());
            await _evaluationFinalizedHandler.Handle(@event, CancellationToken.None);
            A.CallTo(() => _mediator.Send(A<CheckA1CEval>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mapper.Map<A1CEvaluationReceived>(A<EvaluationFinalizedEvent>._)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task EvaluationFinalizedHandler_ProductCodeCaseCheck()
        {
            var barcode = "12345";
            Core.Data.Entities.A1C a1c = null;
            A.CallTo(() => _mediator.Send(A<CheckA1CEval>._, CancellationToken.None)).Returns(barcode);
            A.CallTo(() => _mediator.Send(A<GetA1C>._, CancellationToken.None)).Returns(a1c);
            A.CallTo(() => _mapper.Map<A1CEvaluationReceived>(A<EvaluationFinalizedEvent>._)).Returns(_entityFixtures.MockEvalReceived());
            await _evaluationFinalizedHandler.Handle(@event, CancellationToken.None);
            _endpointInstance.PublishedMessages.Length.Should().Be(1, "Each request will publish one message");
        }

        public static EvaluationFinalizedEvent @event => new EvaluationFinalizedEvent()
        {
            Id = new Guid(),
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084716,
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfService = DateTime.UtcNow,
            DocumentPath = null,
            EvaluationId = 324359,
            EvaluationTypeId = 1,
            FormVersionId = 0,
            Location = new Location(32.925496267, 32.925496267),
            MemberId = 11990396,
            MemberPlanId = 21074285,
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            UserName = "vastest1",
            Products = new List<Product>() { new Product("HBA1C"), new Product("HBA1C") }
        };

    }
}
