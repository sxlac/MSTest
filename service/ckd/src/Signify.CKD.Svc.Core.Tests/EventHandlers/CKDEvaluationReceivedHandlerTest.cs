using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.ApiClient.Response;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.EventHandlers;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Models;
using Signify.CKD.Svc.Core.Queries;
using Signify.CKD.Svc.Core.Tests.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using PublishResult = Signify.CKD.Svc.Core.Commands.PublishResult;
using Result = Signify.CKD.Svc.Core.Messages.Result;

namespace Signify.CKD.Svc.Core.Tests.EventHandlers
{
    public sealed class CKDEvaluationReceivedHandlerTest : IClassFixture<EntityFixtures>, IAsyncDisposable, IDisposable
    {
        private readonly IMediator _mediator = A.Fake<IMediator>();
        private readonly IMapper _mapper = A.Fake<IMapper>();
        private readonly IObservabilityService _observabilityService = A.Fake<IObservabilityService>();
        private readonly EntityFixtures _entityFixtures;
        private readonly CKDEvaluationReceivedHandler _subject;
        private readonly FakeTransactionSupplier _transactionSupplier = new();
        private readonly TestableMessageHandlerContext _context = new();
        private readonly MockDbFixture _dbFixture = new();

        public CKDEvaluationReceivedHandlerTest(EntityFixtures entityFixtures)
        {
            _entityFixtures = entityFixtures;
            _subject =
                new CKDEvaluationReceivedHandler(A.Dummy<ILogger<CKDEvaluationReceivedHandler>>(), _mediator, _mapper, _observabilityService, _transactionSupplier);

            // I'm not refactoring all these tests right now to mock the GetCKD call...
            A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
                .ReturnsLazily(call =>
                {
                    var query = call.Arguments.Get<GetCKD>(0);
                    return Task.FromResult(_dbFixture.Context.CKD.FirstOrDefault(each => each.EvaluationId == query.EvaluationId));
                });
        }

        public ValueTask DisposeAsync()
        {
            return _dbFixture.DisposeAsync();
        }

        public void Dispose()
        {
            _dbFixture.Dispose();
        }

        [Fact]
        public async Task EvaluationFinalizedHandler_WhenDateOfServiceChanges_DateOfServiceIsUpdated()
        {
            var @event = new EvalReceived
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

            await _subject.Handle(@event, _context);

            Assert.Single(_context.SentMessages);
            var message = _context.FindSentMessage<DateOfServiceUpdated>();

            Assert.NotNull(message);
            Assert.Equal(324357, message.EvaluationId);
            Assert.Equal(@event.DateOfService.Value, message.DateOfService);
        }

        [Fact]
        public async Task EvaluationFinalizedHandler_PublishCheck()
        {
            EvaluationAnswers evaluationAnswers = new EvaluationAnswers()
            { LookupCKDAnswerEntity = new LookupCKDAnswer { CKDAnswerValue = "Albumin 80 - Creatinine 0 1" }, ExpirationDate = DateTime.UtcNow, IsCKDEvaluation = true };
            GetMemberInfo memberInfo = new GetMemberInfo() { MemberPlanId = EvalReceived.MemberPlanId };
            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, CancellationToken.None)).Returns(evaluationAnswers);
            A.CallTo(() => _mapper.Map<CreateOrUpdateCKD>(A<EvaluationAnswers>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdateCKD>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
            A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
            A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateCKD>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mediator.Send(A<CreateOrUpdateCKD>._, CancellationToken.None)).Returns(CKD);
            A.CallTo(() => _mediator.Send(A<CKDStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockCKDStatus());
            A.CallTo(() => _mapper.Map<CKDPerformed>(A<Core.Data.Entities.CKD>._)).Returns(_entityFixtures.MockCKDPerformed());
            await _subject.Handle(EvalReceived, _context);
            _context.SentMessages.Length.Should().Be(1);
        }

        [Fact]
        public async Task EvaluationFinalizedHandler_PublishTypeCheck()
        {
            EvaluationAnswers evaluationAnswers = new EvaluationAnswers()
            { LookupCKDAnswerEntity = new LookupCKDAnswer { CKDAnswerValue = "Albumin 80 - Creatinine 0 1" }, ExpirationDate = DateTime.UtcNow, IsCKDEvaluation = true };
            GetMemberInfo memberInfo = new GetMemberInfo() { MemberPlanId = EvalReceived.MemberPlanId };
            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, CancellationToken.None)).Returns(evaluationAnswers);
            A.CallTo(() => _mapper.Map<CreateOrUpdateCKD>(A<EvaluationAnswers>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdateCKD>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
            A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
            A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateCKD>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mediator.Send(A<CreateOrUpdateCKD>._, CancellationToken.None)).Returns(CKD);
            A.CallTo(() => _mediator.Send(A<CKDStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockCKDStatus());
            A.CallTo(() => _mapper.Map<CKDPerformed>(A<Core.Data.Entities.CKD>._)).Returns(_entityFixtures.MockCKDPerformed());
            await _subject.Handle(EvalReceived, _context);
            _context.SentMessages[0].Message.Should().BeOfType<CKDPerformed>();
        }

        [Fact]
        public async Task EvaluationFinalizedHandler_MembersApiTimesCalled()
        {
            EvaluationAnswers evaluationAnswers = new EvaluationAnswers()
            { LookupCKDAnswerEntity = new LookupCKDAnswer { CKDAnswerValue = "Albumin 80 - Creatinine 0 1" }, ExpirationDate = DateTime.UtcNow, IsCKDEvaluation = true };
            GetMemberInfo memberInfo = new GetMemberInfo() { MemberPlanId = EvalReceived.MemberPlanId };
            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, CancellationToken.None)).Returns(evaluationAnswers);
            A.CallTo(() => _mapper.Map<CreateOrUpdateCKD>(A<EvaluationAnswers>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdateCKD>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
            A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
            A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateCKD>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mediator.Send(A<CreateOrUpdateCKD>._, CancellationToken.None)).Returns(CKD);
            A.CallTo(() => _mediator.Send(A<CKDStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockCKDStatus());
            A.CallTo(() => _mapper.Map<CKDPerformed>(A<Core.Data.Entities.CKD>._)).Returns(_entityFixtures.MockCKDPerformed());
            await _subject.Handle(EvalReceived, _context);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }
        [Fact]
        public async Task EvaluationFinalizedHandler_EvaluationApiTimesCalled()
        {
            EvaluationAnswers evaluationAnswers = new EvaluationAnswers()
            { LookupCKDAnswerEntity = new LookupCKDAnswer { CKDAnswerValue = "Albumin 80 - Creatinine 0 1" }, ExpirationDate = DateTime.UtcNow, IsCKDEvaluation = true };
            GetMemberInfo memberInfo = new GetMemberInfo() { MemberPlanId = EvalReceived.MemberPlanId };
            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, CancellationToken.None)).Returns(evaluationAnswers);
            A.CallTo(() => _mapper.Map<CreateOrUpdateCKD>(A<EvaluationAnswers>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdateCKD>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
            A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
            A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateCKD>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mediator.Send(A<CreateOrUpdateCKD>._, CancellationToken.None)).Returns(CKD);
            A.CallTo(() => _mediator.Send(A<CKDStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockCKDStatus());
            A.CallTo(() => _mapper.Map<CKDPerformed>(A<Core.Data.Entities.CKD>._)).Returns(_entityFixtures.MockCKDPerformed());
            await _subject.Handle(EvalReceived, _context);
            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }
        [Fact]
        public async Task EvaluationFinalizedHandler_ProviderApiTimesCalled()
        {
            EvaluationAnswers evaluationAnswers = new EvaluationAnswers()
            { LookupCKDAnswerEntity = new LookupCKDAnswer { CKDAnswerValue = "Albumin 80 - Creatinine 0 1" }, ExpirationDate = DateTime.UtcNow, IsCKDEvaluation = true };
            GetMemberInfo memberInfo = new GetMemberInfo() { MemberPlanId = EvalReceived.MemberPlanId };
            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, CancellationToken.None)).Returns(evaluationAnswers);
            A.CallTo(() => _mapper.Map<CreateOrUpdateCKD>(A<EvaluationAnswers>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdateCKD>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
            A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
            A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateCKD>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mediator.Send(A<CreateOrUpdateCKD>._, CancellationToken.None)).Returns(CKD);
            A.CallTo(() => _mediator.Send(A<CKDStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockCKDStatus());
            A.CallTo(() => _mapper.Map<CKDPerformed>(A<Core.Data.Entities.CKD>._)).Returns(_entityFixtures.MockCKDPerformed());
            await _subject.Handle(EvalReceived, _context);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task EvaluationFinalizedHandler_PublishResultsCalled()
        {
            //Arrange
            EvaluationAnswers evaluationAnswers = new EvaluationAnswers()
            { LookupCKDAnswerEntity = new LookupCKDAnswer { CKDAnswerValue = "Albumin 80 - Creatinine 0 1" }, ExpirationDate = DateTime.UtcNow, IsCKDEvaluation = true };
            GetMemberInfo memberInfo = new GetMemberInfo() { MemberPlanId = EvalReceived.MemberPlanId };
            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, CancellationToken.None)).Returns(evaluationAnswers);
            A.CallTo(() => _mapper.Map<CreateOrUpdateCKD>(A<EvaluationAnswers>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mapper.Map(A<EvalReceived>._, A<CreateOrUpdateCKD>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mediator.Send(A<GetProviderInfo>._, CancellationToken.None)).Returns(_entityFixtures.MockProviderRs());
            A.CallTo(() => _mapper.Map<GetMemberInfo>(A<EvalReceived>._)).Returns(memberInfo);
            A.CallTo(() => _mediator.Send(A<GetMemberInfo>._, CancellationToken.None)).Returns(MemberInfoRs);
            A.CallTo(() => _mapper.Map(A<MemberInfoRs>._, A<CreateOrUpdateCKD>._)).Returns(GetCreateOrUpdateCkd);
            A.CallTo(() => _mediator.Send(A<CreateOrUpdateCKD>._, CancellationToken.None)).Returns(CKD);
            A.CallTo(() => _mediator.Send(A<CKDStatus>._, CancellationToken.None)).Returns(_entityFixtures.MockCKDStatus());
            A.CallTo(() => _mapper.Map<CKDPerformed>(A<Core.Data.Entities.CKD>._)).Returns(_entityFixtures.MockCKDPerformed());
            A.CallTo(() => _mapper.Map<Result>(A<Core.Data.Entities.CKD>._)).Returns(new Result());
            A.CallTo(() => _mapper.Map(A<Core.Data.Entities.LookupCKDAnswer>._, A<Result>._)).Returns(new Result());
            A.CallTo(() => _mediator.Send(A<PublishResult>._, CancellationToken.None)).Returns(Unit.Value);

            //Act
            await _subject.Handle(EvalReceived, _context);

            //Assert
            A.CallTo(() => _mapper.Map<Result>(A<Core.Data.Entities.CKD>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mapper.Map(A<Core.Data.Entities.LookupCKDAnswer>._, A<Result>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mediator.Send(A<PublishResult>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task EvaluationFinalizedHandler_PublishResultsCalled_And_CkdAnswer_Is_Null_And_Exam_Is_Performed()
        {
            //Arrange
            EvaluationAnswers evaluationAnswers = new EvaluationAnswers()
            { LookupCKDAnswerEntity = null, ExpirationDate = DateTime.UtcNow, IsCKDEvaluation = true };
            A.CallTo(() => _mapper.Map<Result>(A<Core.Data.Entities.CKD>._)).Returns(new Result());
            A.CallTo(() => _mapper.Map(A<Core.Data.Entities.LookupCKDAnswer>._, A<Result>._)).Returns(new Result());
            A.CallTo(() => _mediator.Send(A<PublishResult>._, CancellationToken.None)).Returns(Unit.Value);
            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, CancellationToken.None)).Returns(evaluationAnswers);

            //Act
            await _subject.Handle(EvalReceived, _context);

            //Assert
            A.CallTo(() => _mapper.Map<Result>(A<Core.Data.Entities.CKD>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mapper.Map(A<Core.Data.Entities.LookupCKDAnswer>._, A<Result>._)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mediator.Send(A<PublishResult>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task EvaluationFinalizedHandler_PublishResults_Not_Called_When_Is_Not_Performed()
        {
            //Arrange
            EvaluationAnswers evaluationAnswers = new EvaluationAnswers()
            { LookupCKDAnswerEntity = null, ExpirationDate = DateTime.UtcNow, IsCKDEvaluation = false, NotPerformedReasonId = 12345, NotPerformedNotes = "Unable To Perform" };
            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, CancellationToken.None)).Returns(evaluationAnswers);
            A.CallTo(() => _mapper.Map<Result>(A<Core.Data.Entities.CKD>._)).Returns(new Result());
            A.CallTo(() => _mapper.Map(A<Core.Data.Entities.LookupCKDAnswer>._, A<Result>._)).Returns(new Result());
            A.CallTo(() => _mediator.Send(A<PublishResult>._, CancellationToken.None)).Returns(Unit.Value);

            //Act
            await _subject.Handle(EvalReceived, _context);

            //Assert
            A.CallTo(() => _mapper.Map<Result>(A<Core.Data.Entities.CKD>._)).MustNotHaveHappened();
            A.CallTo(() => _mapper.Map(A<Core.Data.Entities.LookupCKDAnswer>._, A<Result>._)).MustNotHaveHappened();
            A.CallTo(() => _mediator.Send(A<PublishResult>._, CancellationToken.None)).MustNotHaveHappened();
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

        private static EvalReceived EvalReceived => new EvalReceived()
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
            UserName = "vastest1"
        };
        private static CreateOrUpdateCKD GetCreateOrUpdateCkd => new CreateOrUpdateCKD()
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
            CKDAnswer = "Albumin 80 - Creatinine 0 1",
            CenseoId = "ADarsh1234",
            City = "Mysuru",
            DateOfBirth = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow,
            FirstName = "Adarsh",
            LastName = "H R",
            NationalProviderIdentifier = "1234567890",
            State = "karnataka"
        };

        private static Core.Data.Entities.CKD CKD => new Core.Data.Entities.CKD()
        {
            CKDId = +10,
            AddressLineOne = "4420 Harpers Ferry Dr",
            AddressLineTwo = "Harpers Ferry Dr",
            ApplicationId = "Signify.Evaluation.Service",
            AppointmentId = 1000084715,
            CKDAnswer = "Albumin 80 - Creatinine 0 1",
            CenseoId = "Adarsh1234",
            City = "Mysuru",
            ClientId = 14,
            CreatedDateTime = DateTimeOffset.UtcNow,
            DateOfBirth = DateTime.UtcNow,
            DateOfService = DateTime.UtcNow,
            EvaluationId = 324356,
            ExpirationDate = DateTime.UtcNow,
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

        [Fact]
        public async Task Handle_WithNewEval_WhereExamPerformed_PublishesPerformed()
        {
            var eventId = Guid.NewGuid();

            var entity = new Core.Data.Entities.CKD();

            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, A<CancellationToken>._))
                .Returns(new EvaluationAnswers
                {
                    IsCKDEvaluation = true
                });

            A.CallTo(() => _mediator.Send(A<CreateOrUpdateCKD>._, A<CancellationToken>._))
                .Returns(entity);

            await _subject.Handle(new EvalReceived
            {
                Id = eventId
            }, _context);

            A.CallTo(() => _mapper.Map<CKDPerformed>(A<Core.Data.Entities.CKD>._))
                .MustHaveHappened();

            A.CallTo(() => _mediator.Send(A<CreateCKDStatus>.That.Matches(q => q.StatusCodeId == CKDStatusCode.CKDPerformed.CKDStatusCodeId),
                A<CancellationToken>._)).MustHaveHappened();

            Assert.Single(_context.SentMessages);

            Assert.NotNull(_context.FindSentMessage<CKDPerformed>());
        }

        [Fact]
        public async Task Handle_WithNewEval_WhereExamNotPerformed_PublishesNotPerformed()
        {
            const int notPerformedReasonId = 1;

            var eventId = Guid.NewGuid();

            var entity = new Core.Data.Entities.CKD();

            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, A<CancellationToken>._))
                .Returns(new EvaluationAnswers
                {
                    IsCKDEvaluation = false,
                    NotPerformedReasonId = notPerformedReasonId
                });

            A.CallTo(() => _mediator.Send(A<CreateOrUpdateCKD>._, A<CancellationToken>._))
                .Returns(entity);

            await _subject.Handle(new EvalReceived
            {
                Id = eventId
            }, _context);

            A.CallTo(() => _mapper.Map<CKDPerformed>(A<Core.Data.Entities.CKD>._))
                .MustNotHaveHappened();

            Assert.Single(_context.SentMessages);

            var publishedMessage = _context.FindSentMessage<ExamNotPerformedEvent>();
            Assert.Same(entity, publishedMessage.Exam);
            Assert.Equal(eventId, publishedMessage.EventId);
            Assert.Equal(notPerformedReasonId, publishedMessage.NotPerformedReasonId);
        }

        [Fact]
        public async Task Handle_WhenCkdNotFound_QueriesEvaluationAnswers()
        {
            // Arrange
            const int evaluationId = 1;

            A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
                .Returns(Task.FromResult<Core.Data.Entities.CKD>(null));

            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, A<CancellationToken>._))
                .Returns(new EvaluationAnswers
                {
                    IsCKDEvaluation = true
                });

            A.CallTo(() => _mediator.Send(A<CreateOrUpdateCKD>._, A<CancellationToken>._))
                .Returns(new Core.Data.Entities.CKD
                {
                    EvaluationId = evaluationId
                });

            // Act
            await _subject.Handle(new EvalReceived { EvaluationId = evaluationId }, _context);

            // Assert
            A.CallTo(() => _mediator.Send(A<GetCKD>.That.Matches(q => q.EvaluationId == evaluationId),
                    A<CancellationToken>._))
                .MustHaveHappened();

            A.CallTo(() => _mediator.Send(A<CheckCKDEval>.That.Matches(q => q.EvaluationId == evaluationId),
                    A<CancellationToken>._))
                .MustHaveHappened();

            Assert.Null(_context.FindPublishedMessage<DateOfServiceUpdated>());
        }

        [Fact]
        public async Task Handle_HappyPath_CommitsTransaction()
        {
            // Arrange
            const int evaluationId = 1;

            A.CallTo(() => _mediator.Send(A<GetCKD>._, A<CancellationToken>._))
                .Returns(Task.FromResult<Core.Data.Entities.CKD>(null));

            A.CallTo(() => _mediator.Send(A<CheckCKDEval>._, A<CancellationToken>._))
                .Returns(new EvaluationAnswers
                {
                    IsCKDEvaluation = true
                });

            A.CallTo(() => _mediator.Send(A<CreateOrUpdateCKD>._, A<CancellationToken>._))
                .Returns(new Core.Data.Entities.CKD
                {
                    EvaluationId = evaluationId
                });

            // Act
            await _subject.Handle(new EvalReceived { EvaluationId = evaluationId }, _context);

            // Assert
            _transactionSupplier.AssertCommit();
        }
    }
}
