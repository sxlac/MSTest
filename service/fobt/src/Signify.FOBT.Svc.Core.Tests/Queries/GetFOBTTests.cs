using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetFOBTTests : IClassFixture<MockDbFixture>
{
	private readonly GetFOBTHandler _handler;

	public GetFOBTTests(MockDbFixture mockDbFixture)
	{
		var logger = A.Fake<ILogger<GetFOBTHandler>>();
		_handler = new GetFOBTHandler(mockDbFixture.Context, logger);
	}

	[Theory]
	[InlineData(324357)]
	[InlineData(324358)]
	[InlineData(324356)]
	public async Task GetFobt_ReturnsOneResult_Successful(int evaluationId)
	{
		var query = new GetFOBT { EvaluationId = evaluationId };
		var result = await _handler.Handle(query, CancellationToken.None);
		Assert.NotNull(result);
		Assert.True(result.EvaluationId.Equals(evaluationId));
	}


	[Theory]
	[InlineData(1231)]
	[InlineData(1232)]
	[InlineData(1233)]
	public async Task GetFobt_Type_DataNotfound(int evaluationId)
	{
		var query = new GetFOBT { EvaluationId = evaluationId };
		var result = await _handler.Handle(query, CancellationToken.None);
		result.Should().BeNull();
	}
	[Theory]
	[InlineData(324357)]
	[InlineData(324358)]
	[InlineData(324356)]
	public async Task GetFobt_Type_DataFound(int evaluationId)
	{
		var query = new GetFOBT { EvaluationId = evaluationId };
		var result = await _handler.Handle(query, CancellationToken.None);
		result.Should().BeOfType<Core.Data.Entities.FOBT>();
	}

	[Theory]
	[InlineData(1231)]
	[InlineData(1232)]
	[InlineData(1233)]
	public async Task GetFobt_Returns_NullResult(int evaluationId)
	{
		var query = new GetFOBT { EvaluationId = evaluationId };
		var result = await _handler.Handle(query, CancellationToken.None);
		Assert.Null(result);
	}
}