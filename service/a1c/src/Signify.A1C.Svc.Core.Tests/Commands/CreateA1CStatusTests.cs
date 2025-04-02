using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Data.Entities;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.Commands
{
    public class CreateA1CStatusTests : IClassFixture<MockA1CDBFixture>
    {
        private readonly CreateA1CStatusHandler _createA1CStatusHandler;
        private readonly MockA1CDBFixture _moA1CbFixture;

        public CreateA1CStatusTests(MockA1CDBFixture moA1CbFixture)
        {
            _moA1CbFixture = moA1CbFixture;
            _createA1CStatusHandler = new CreateA1CStatusHandler(_moA1CbFixture.Context);
        }

        [Fact]
        public async Task Should_Insert_A1C_Status()
        {
            var A1C = new Core.Data.Entities.A1C();
            var status = new CreateA1CStatus
            {
                StatusCode = A1CStatusCode.A1CPerformed,
                A1CId = A1C.A1CId
            };

            //Act
            var result = await _createA1CStatusHandler.Handle(status, CancellationToken.None);

            //Assert
            result.Should().NotBe(null);
        }

        [Fact]
        public async Task Should_Insert_A1C_Status_Count()
        {
            var count = _moA1CbFixture.Context.A1CStatus.Count();
            var A1C = new Core.Data.Entities.A1C();
            var status = new CreateA1CStatus
            {
                StatusCode = A1CStatusCode.A1CPerformed,
                A1CId = A1C.A1CId
            };

            //Act
            var result = await _createA1CStatusHandler.Handle(status, CancellationToken.None);

            //Assert
            _moA1CbFixture.Context.A1CStatus.Count().Should().BeGreaterThan(count);
        }

        [Fact]
        public void Should_Compare_A1C_Instances_Success()
        {
            var A1C = new Core.Data.Entities.A1C();
            A1C.A1CId = 21;
            var A1C2 = new Core.Data.Entities.A1C();
            A1C2.A1CId = 21;

            //Act
            var result = A1C.Equals(A1C2);

            //Assert
            Assert.True(result);
        }
    }
}
