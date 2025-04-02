using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.ApiClient.Response;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Data.Entities;
using Signify.A1C.Svc.Core.EventHandlers;
//using Signify.A1C.Svc.Core.Models;
using Signify.A1C.Svc.Core.Queries;
using Signify.A1C.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.EventHandlers
{
    public class A1CEvaluationReceivedHandlerTest : IClassFixture<EntityFixtures>, IClassFixture<MockA1CDBFixture>
    {
        private readonly ILogger<A1CEvaluationReceivedHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly EntityFixtures _entityFixtures;
        private readonly A1CEvaluationReceivedHandler _A1CEvaluationReceivedHandler;
        private readonly TestableMessageHandlerContext _messageHandlerContext;
        public A1CEvaluationReceivedHandlerTest(EntityFixtures entityFixtures, MockA1CDBFixture moA1CbFixture)
        {
            _logger = A.Fake<ILogger<A1CEvaluationReceivedHandler>>();
            _mediator = A.Fake<IMediator>();
            _mapper = A.Fake<IMapper>();
            _entityFixtures = entityFixtures;
            _messageHandlerContext = new TestableMessageHandlerContext();
            _A1CEvaluationReceivedHandler =
                new A1CEvaluationReceivedHandler(_logger, _mediator, moA1CbFixture.Context, _mapper);
        }

        [Fact]
        public async Task Should_Ignore_WhenEvaluationIsAlreadyFinalized()
        {

            A1CEvaluationReceived  @event = new A1CEvaluationReceived()
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
                MemberId = 11990396,
                MemberPlanId = 21074285,
                ProviderId = 42879,
                ReceivedDateTime = DateTime.UtcNow,
                UserName = "vastest1"
            };
            await _A1CEvaluationReceivedHandler.Handle(@event, _messageHandlerContext);
            _messageHandlerContext.PublishedMessages.Length.Should().Be(0);
        }
        [Fact]
        public async Task Should_Publish_A1CPerformedEvent()
        {
            GetMemberInfo memberInfo = new GetMemberInfo() { MemberPlanId = EvalReceived.MemberPlanId };
            A.CallTo(() => _mediator.Send(A<QueryA1C>._, CancellationToken.None)).Returns(_entityFixtures.MockQueryA1CResponse());
            A.CallTo(() => _mapper.Map(A<A1CEvaluationReceived>._, A<CreateOrUpdateA1C>._)).Returns(GetCreateOrUpdateA1C);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
            A.CallTo(() => _mapper.Map<GetMemberInfo>(A<A1CEvaluationReceived>._)).Returns(memberInfo);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
            A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateA1C>._)).Returns(GetCreateOrUpdateA1C);
            A.CallTo(() => _mediator.Send(A<CreateOrUpdateA1C>._, CancellationToken.None)).Returns(A1C);
            A.CallTo(() => _mediator.Send(A<A1CStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockA1CStatus());
            A.CallTo(() => _mapper.Map<A1CPerformedEvent>(A<Core.Data.Entities.A1C>._)).Returns(_entityFixtures.MockA1CPerformed());
            await _A1CEvaluationReceivedHandler.Handle(EvalReceived, _messageHandlerContext);
            _messageHandlerContext.PublishedMessages.Length.Should().Be(1);
        }

        [Fact]
        public async Task EvaluationReceivedHandler_PublishTypeCheck()
        {
            GetMemberInfo memberInfo = new GetMemberInfo() { MemberPlanId = EvalReceived.MemberPlanId };
            A.CallTo(() => _mediator.Send(A<QueryA1C>._, CancellationToken.None)).Returns(_entityFixtures.MockQueryA1CResponse());
            A.CallTo(() => _mapper.Map(A<A1CEvaluationReceived>._, A<CreateOrUpdateA1C>._)).Returns(GetCreateOrUpdateA1C);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
            A.CallTo(() => _mapper.Map<GetMemberInfo>(A<A1CEvaluationReceived>._)).Returns(memberInfo);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
            A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateA1C>._)).Returns(GetCreateOrUpdateA1C);
            A.CallTo(() => _mediator.Send(A<CreateOrUpdateA1C>._, CancellationToken.None)).Returns(A1C);
            A.CallTo(() => _mediator.Send(A<A1CStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockA1CStatus());
            A.CallTo(() => _mapper.Map<A1CPerformedEvent>(A<Core.Data.Entities.A1C>._)).Returns(_entityFixtures.MockA1CPerformed());
            await _A1CEvaluationReceivedHandler.Handle(EvalReceived, _messageHandlerContext);
            _messageHandlerContext.PublishedMessages[0].Message.Should().BeOfType<A1CPerformedEvent>();
        }

        [Fact]
        public async Task EvaluationReceivedHandler_MembersApiTimesCalled()
        {
            GetMemberInfo memberInfo = new GetMemberInfo() { MemberPlanId = EvalReceived.MemberPlanId };
            A.CallTo(() => _mediator.Send(A<QueryA1C>._, CancellationToken.None)).Returns(_entityFixtures.MockQueryA1CResponse());
            A.CallTo(() => _mapper.Map(A<A1CEvaluationReceived>._, A<CreateOrUpdateA1C>._)).Returns(GetCreateOrUpdateA1C);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
            A.CallTo(() => _mapper.Map<GetMemberInfo>(A<A1CEvaluationReceived>._)).Returns(memberInfo);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
            A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateA1C>._)).Returns(GetCreateOrUpdateA1C);
            A.CallTo(() => _mediator.Send(A<CreateOrUpdateA1C>._, CancellationToken.None)).Returns(A1C);
            A.CallTo(() => _mediator.Send(A<A1CStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockA1CStatus());
            A.CallTo(() => _mapper.Map<A1CPerformedEvent>(A<Core.Data.Entities.A1C>._)).Returns(_entityFixtures.MockA1CPerformed());
            await _A1CEvaluationReceivedHandler.Handle(EvalReceived, _messageHandlerContext);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }
        [Fact]
        public async Task EvaluationReceivedHandler_ProviderApiTimesCalled()
        {
            GetMemberInfo memberInfo = new GetMemberInfo() { MemberPlanId = EvalReceived.MemberPlanId };
            A.CallTo(() => _mediator.Send(A<QueryA1C>._, CancellationToken.None)).Returns(_entityFixtures.MockQueryA1CResponse());
            A.CallTo(() => _mapper.Map(A<A1CEvaluationReceived>._, A<CreateOrUpdateA1C>._)).Returns(GetCreateOrUpdateA1C);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
            A.CallTo(() => _mapper.Map<GetMemberInfo>(A<A1CEvaluationReceived>._)).Returns(memberInfo);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
            A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateA1C>._)).Returns(GetCreateOrUpdateA1C);
            A.CallTo(() => _mediator.Send(A<CreateOrUpdateA1C>._, CancellationToken.None)).Returns(A1C);
            A.CallTo(() => _mediator.Send(A<A1CStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockA1CStatus());
            A.CallTo(() => _mapper.Map<A1CPerformedEvent>(A<Core.Data.Entities.A1C>._)).Returns(_entityFixtures.MockA1CPerformed());
            await _A1CEvaluationReceivedHandler.Handle(EvalReceived, _messageHandlerContext);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }
        [Fact]
        public async Task EvaluationReceivedHandler_WhenA1CCreationFailed()
        {
            GetMemberInfo memberInfo = new GetMemberInfo() { MemberPlanId = EvalReceived.MemberPlanId };
            A.CallTo(() => _mediator.Send(A<QueryA1C>._, CancellationToken.None)).Returns(_entityFixtures.MockQueryA1CResponse());
            A.CallTo(() => _mapper.Map(A<A1CEvaluationReceived>._, A<CreateOrUpdateA1C>._)).Returns(GetCreateOrUpdateA1C);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
            A.CallTo(() => _mapper.Map<GetMemberInfo>(A<A1CEvaluationReceived>._)).Returns(memberInfo);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
            A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateA1C>._)).Returns(GetCreateOrUpdateA1C);
            A.CallTo(() => _mediator.Send(A<CreateOrUpdateA1C>._, CancellationToken.None)).Returns(new Core.Data.Entities.A1C());
            A.CallTo(() => _mediator.Send(A<A1CStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockA1CStatus());
            A.CallTo(() => _mapper.Map<A1CPerformedEvent>(A<Core.Data.Entities.A1C>._)).Returns(_entityFixtures.MockA1CPerformed());
            await _A1CEvaluationReceivedHandler.Handle(EvalReceived, _messageHandlerContext);
            _messageHandlerContext.PublishedMessages.Length.Should().Be(0);
        }
        private static MemberInfoRs MemberInfoRs => new MemberInfoRs()
        {
            AddressLineOne = "Raghavendra nagara",
            AddressLineTwo = "mysuru",
            CenseoId = "ADarsh1234",
            City = "Mysuru",
            DateOfBirth = DateTime.UtcNow,
            FirstName = "Adarsh",
            LastName = "H R",
            State = "karnataka",
            ZipCode = "12345",
            Client = "14",
            MiddleName = ""
        };

        private static A1CEvaluationReceived EvalReceived => new A1CEvaluationReceived()
        {
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084716,
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfService = DateTime.UtcNow,
            DocumentPath = null,
            EvaluationId = 324359,
            EvaluationTypeId = 1,
            FormVersionId = 0,
            MemberId = 11990396,
            MemberPlanId = 21074285,
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            UserName = "vastest1",
            Performed = true
        };
        private static CreateOrUpdateA1C GetCreateOrUpdateA1C => new CreateOrUpdateA1C()
        {
            AddressLineOne = "Raghavendra nagara",
            AddressLineTwo = "mysuru",
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084716,
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfService = DateTime.UtcNow,
            EvaluationId = 324359,
            MemberId = 11990396,
            MemberPlanId = 21074285,
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            CenseoId = "ADarsh1234",
            City = "Mysuru",
            DateOfBirth = DateTime.UtcNow,
            FirstName = "Adarsh",
            LastName = "H R",
            NationalProviderIdentifier = "1234567890",
            State = "karnataka"
        };

        private static Core.Data.Entities.A1C A1C => new Core.Data.Entities.A1C()
        {
            A1CId = +10,
            AddressLineOne = "4420 Harpers Ferry Dr",
            AddressLineTwo = "Harpers Ferry Dr",
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084715,
            CenseoId = "Adarsh1234",
            City = "Mysuru",
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfBirth = DateTime.UtcNow,
            DateOfService = DateTime.UtcNow,
            EvaluationId = 324356,
            FirstName = "Adarsh",
            LastName = "H R",
            MemberId = 11990396,
            MemberPlanId = 21074285,
            NationalProviderIdentifier = "9230239051",
            ProviderId = 42879,
            ReceivedDateTime = DateTime.UtcNow,
            State = "Karnataka",
            UserName = "vastest1",
            ZipCode = "12345"
        };
    }
}
