using FluentAssertions;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Queries;

public class GetHba1CPocStatusHandlerTests : IClassFixture<MockDbFixture>
{
    private readonly GetHba1CPocStatusHandler _handler;

    public GetHba1CPocStatusHandlerTests(MockDbFixture mockDbFixture)
    {
        _handler = new GetHba1CPocStatusHandler(mockDbFixture.Context);
    }

    [Theory]
    [InlineData(324357)]
    [InlineData(324358)]
    [InlineData(324356)]
    public async Task GetHba1CPocStatusHandler_RequestCorrectEvaluationIdAndStatusCode_SuccessfulResponse(int evaluationId)
    {
        // Arrange
        var query = new GetHba1CPocStatus { EvaluationId = evaluationId, StatusCode = HBA1CPOCStatusCode.HBA1CPOCPerformed };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result.HBA1CPOC.EvaluationId, evaluationId);
    }

    [Theory]
    [InlineData(324357)]
    [InlineData(324358)]
    [InlineData(324356)]
    public async Task GetHba1CPocStatusHandler_RequestCorrectEvaluationIdAndInvalidStatusCode_NullResponse(int evaluationId)
    {
        // Arrange
        var query = new GetHba1CPocStatus { EvaluationId = evaluationId, StatusCode = HBA1CPOCStatusCode.BillRequestSent };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(1231)]
    [InlineData(1232)]
    [InlineData(1233)]
    public async Task GetHba1CPocStatusHandler_RequestIncorrectEvaluationIdAndValidStatusCode_NullResponse(int evaluationId)
    {
        // Arrange
        var query = new GetHba1CPocStatus { EvaluationId = evaluationId, StatusCode = HBA1CPOCStatusCode.HBA1CPOCPerformed };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}