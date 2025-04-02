using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetLatestCdiEventTests : IClassFixture<MockDbFixture>
{
	private readonly GetLatestCdiEventHandler _handler;
	private readonly IMediator _mediator = A.Fake<IMediator>();

	public GetLatestCdiEventTests(MockDbFixture mockDbFixture)
	{
		_handler = new GetLatestCdiEventHandler(A.Fake<ILogger<GetLatestCdiEventHandler>>(), mockDbFixture.Context);
	}

	[Theory]
	[InlineData(1231)]
	[InlineData(1232)]
	[InlineData(1233)]
	public async Task GetLatestCdiEventTests_Null_NoData(int evaluationId)
	{
		var query = new GetLatestCdiEvent { EvaluationId = evaluationId };
		var result = await _handler.Handle(query, CancellationToken.None);
		result.Should().BeNull();
	}


	[Theory]
	[MemberData(nameof(GetValidData))]
	public async Task GetLatestCdiEventTests_NotNull_DataType_ValidData(FOBTStatus fobtStatus)
	{
		//Arrange
		var query = new GetLatestCdiEvent { EvaluationId = 1 };
		A.CallTo(() => _mediator.Send(A<GetLatestCdiEvent>._, A<CancellationToken>._))
			.Returns(fobtStatus);
		//Act
		var result = await _mediator.Send(query, CancellationToken.None);
		//Assert
		result.Should().NotBeNull();
		result.Should().BeOfType(typeof(FOBTStatus));
	}

	public static IEnumerable<object[]> GetValidData()
	{
		var data = new List<object[]>
		{
			new object[]
			{
				CreateFobtStatus(FOBTStatusCode.CdiPassedReceived.FOBTStatusCodeId)
			},
			new object[]
			{
				CreateFobtStatus(FOBTStatusCode.CdiFailedWithPayReceived.FOBTStatusCodeId)
			},
			new object[]
			{
				CreateFobtStatus(FOBTStatusCode.CdiFailedWithoutPayReceived.FOBTStatusCodeId)
			}
		};
		return data;

        static FOBTStatus CreateFobtStatus(int id)
        {
	        return new FOBTStatus
	        {
		        FOBTStatusCodeId = id
	        };
        }
	}
}