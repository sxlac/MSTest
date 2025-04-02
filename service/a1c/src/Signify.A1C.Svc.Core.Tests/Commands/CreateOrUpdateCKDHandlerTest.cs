using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Tests.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.Commands
{
    public class CreateOrUpdateA1CHandlerTest : IClassFixture<EntityFixtures>, IClassFixture<MockA1CDBFixture>
    {
        private readonly IMapper _mapper;
        private readonly ILogger<CreateOrUpdateA1CHandler> _logger;
        private readonly CreateOrUpdateA1CHandler _createOrUpdateA1CHandler;
        private readonly MockA1CDBFixture _moA1CbFixture;
        public CreateOrUpdateA1CHandlerTest(MockA1CDBFixture moA1CbFixture)
        {
            _mapper = A.Fake<IMapper>();
            _logger = A.Fake<ILogger<CreateOrUpdateA1CHandler>>();
            _moA1CbFixture = moA1CbFixture;
            _createOrUpdateA1CHandler = new CreateOrUpdateA1CHandler(moA1CbFixture.Context, _mapper, _logger);
        }

        [Fact]
        public async Task Should_Create_A1C_DataCheck()
        {
            A.CallTo(() => _mapper.Map<Core.Data.Entities.A1C>(A<CreateOrUpdateA1C>._)).Returns(CreateA1C);
            var A1C = await _createOrUpdateA1CHandler.Handle(CreateCreateOrUpdateA1C, CancellationToken.None);
            _moA1CbFixture.Context.A1C.ToList().Any(x => x.AppointmentId == A1C.AppointmentId).Should().BeTrue();
        }
        [Fact]
        public async Task Should_Create_A1C_CountTest()
        {
            A.CallTo(() => _mapper.Map<Core.Data.Entities.A1C>(A<CreateOrUpdateA1C>._)).Returns(CreateA1C);
            var initialCount = _moA1CbFixture.Context.A1C.Count();
            await _createOrUpdateA1CHandler.Handle(CreateCreateOrUpdateA1C, CancellationToken.None);
            _moA1CbFixture.Context.A1C.Count().Should().BeGreaterThan(initialCount, "There shd be an insert");
        }
        [Fact]
        public async Task Should_Create_A1C()
        {
            A.CallTo(() => _mapper.Map<Core.Data.Entities.A1C>(A<CreateOrUpdateA1C>._)).Returns(CreateA1C);
            var initialCount = _moA1CbFixture.Context.A1C.Count();
            var A1C = await _createOrUpdateA1CHandler.Handle(CreateCreateOrUpdateA1C, CancellationToken.None);
            A1C.A1CId.Should().BeGreaterThan(initialCount);
        }

        [Fact]
        public async Task Should_Create_A1C_TypeCheck()
        {
            A.CallTo(() => _mapper.Map<Core.Data.Entities.A1C>(A<CreateOrUpdateA1C>._)).Returns(CreateA1C);
            var initialCount = _moA1CbFixture.Context.A1C.Count();
            var A1C = await _createOrUpdateA1CHandler.Handle(CreateCreateOrUpdateA1C, CancellationToken.None);
            A1C.Should().BeOfType<Core.Data.Entities.A1C>();
        }
        [Fact]
        public async Task Should_Update_A1C_TypeCheck()
        {
            A.CallTo(() => _mapper.Map<Core.Data.Entities.A1C>(A<CreateOrUpdateA1C>._)).Returns(CreateA1C);
            var initialCount = _moA1CbFixture.Context.A1C.Count();
            var A1C = await _createOrUpdateA1CHandler.Handle(CreateCreateOrUpdateA1C, CancellationToken.None);
            A1C.Should().BeOfType<Core.Data.Entities.A1C>();
        }

        [Fact]
        public async Task Should_Update_A1C_DataCheck()
        {
            A.CallTo(() => _mapper.Map<Core.Data.Entities.A1C>(A<CreateOrUpdateA1C>._)).Returns(CreateA1C);
            var initialCount = _moA1CbFixture.Context.A1C.Count();
            var A1C = await _createOrUpdateA1CHandler.Handle(CreateCreateOrUpdateA1C, CancellationToken.None);
            _moA1CbFixture.Context.A1C.Should().Contain(A1C);
        }

        private static Core.Data.Entities.A1C CreateA1C => new Core.Data.Entities.A1C()
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
            State = "karnataka",
            UserName = "ADarsh",
            ZipCode = "12345"
        };

        private static CreateOrUpdateA1C CreateCreateOrUpdateA1C => new CreateOrUpdateA1C()
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
            State = "karnataka",
            UserName = "ADarsh",
            ZipCode = "12345"
        };
    }
}
