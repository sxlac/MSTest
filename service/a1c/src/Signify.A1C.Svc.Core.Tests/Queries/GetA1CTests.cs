using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.A1C.Svc.Core.Queries;
using Signify.A1C.Svc.Core.Tests.Utilities;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.A1C.Svc.Core.Tests.Queries
{
    public class GetA1CTests : IClassFixture<EntityFixtures>, IClassFixture<MockA1CDBFixture>
	{
        private readonly ILogger<GetA1CHandler> _logger;
        private readonly GetA1CHandler _getA1cHandler;
        private readonly EntityFixtures _entityFixtures;

		public GetA1CTests(EntityFixtures entityFixtures, MockA1CDBFixture mockDbFixture)
        {
            _logger = A.Fake<ILogger<GetA1CHandler>>();
            _entityFixtures = entityFixtures;
            _getA1cHandler = new GetA1CHandler(mockDbFixture.Context, _logger);
		}

        
        [Theory]
        [InlineData(324357)]
        [InlineData(324358)]
        [InlineData(324356)]
        public async Task Should_Return_Result(int evaluationId)
        {
            var query = new GetA1C() { EvaluationId = evaluationId };
            var result = await _getA1cHandler.Handle(query, CancellationToken.None);
            Assert.NotNull(result);
            result.A1CId.Equals(evaluationId);
        }

        [Theory]
        [InlineData(1231)]
        [InlineData(1232)]
        [InlineData(1233)]
        public async Task GetA1C_Type_Datanotfound(int evaluationId)
        {
            var query = new GetA1C() { EvaluationId = evaluationId };
            var result = await _getA1cHandler.Handle(query, CancellationToken.None);
            result.Should().BeNull();
        }
        [Theory]
        [InlineData(324357)]
        [InlineData(324358)]
        [InlineData(324356)]
        public async Task Should_Be_Type_A1C(int evaluationId)
        {
            var query = new GetA1C() { EvaluationId = evaluationId };
            var result = await _getA1cHandler.Handle(query, CancellationToken.None);
            result.Should().BeOfType<Core.Data.Entities.A1C>();
        }

        [Theory]
        [InlineData(1231)]
        [InlineData(1232)]
        [InlineData(1233)]
        public async Task Should_Return_Null_Result(int evaluationId)
        {
            var query = new GetA1C() { EvaluationId = evaluationId };
            var result = await _getA1cHandler.Handle(query, CancellationToken.None);
            Assert.Null(result);
        }
    }

}

