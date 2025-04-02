using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Mocks.Json.Queries;
using System.Threading.Tasks;
using System.Threading;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetNotPerformedReasonTest: IClassFixture<MockDbFixture>
{
    private readonly IEvaluationApi _evalApi;
    private readonly GetNotPerformedReasonHandler _handler;

    public GetNotPerformedReasonTest(MockDbFixture mockDbFixture)
    {
        _evalApi = A.Fake<IEvaluationApi>();

        _handler = new GetNotPerformedReasonHandler(_evalApi, mockDbFixture.Context, A.Dummy<ILogger<GetNotPerformedReasonHandler>>());
    }

    [Fact]
    public async Task Handle_EvaluationReasonCapture_ReturnsAnswerValue()
    {
        // Arrange
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<long>._, A<string>._)).Returns(GetApiResponseWithMatchingAnswerId());

        // Act
        var actualResult = await _handler.Handle(GetNotPerformedReason, CancellationToken.None);

        // Assert
        actualResult.NotPerformedReason.AnswerId.Should().Be(NotPerformedReason.MemberRecentlyCompleted.AnswerId);
    }

    [Fact]
    public async Task Handle_EvaluationWithoutReasonAnswer_ReturnsNull()
    {
        // Arrange
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<long>._, A<string>._)).Returns(GetApiResponseWithoutMatchingAnswerId());

        // Act
        var actualResult = await _handler.Handle(GetNotPerformedReason, CancellationToken.None);
            
        // Assert
        actualResult.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EvaluationReason_CaptureReasonAndReasonNoteAndReasonTypeValues()
    {
        // Arrange
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<long>._, A<string>._)).Returns(GetApiResponseWithReasonForMissingTest());

        // Act
        var actualResult = await _handler.Handle(GetNotPerformedReason, CancellationToken.None);
            
        // Assert
        actualResult.Reason.Should().Be(NotPerformedReason.MemberRecentlyCompleted.Reason);
        actualResult.ReasonType.Should().Be(ReasonType.MemberRefusal);
        actualResult.ReasonNotes.Should().Be("Not interested in taking test");
    }

    private static EvaluationVersionRs GetApiResponseWithMatchingAnswerId()
    {
        return JsonConvert.DeserializeObject<EvaluationVersionRs>(QueriesAPIResponse.EvaluationVersionWithMatchingAnswerId);
    }

    private static EvaluationVersionRs GetApiResponseWithoutMatchingAnswerId()
    {
        return JsonConvert.DeserializeObject<EvaluationVersionRs>(QueriesAPIResponse.EVALUATIONVERSION);
    }

    private static EvaluationVersionRs GetApiResponseWithReasonForMissingTest()
    {
        return JsonConvert.DeserializeObject<EvaluationVersionRs>(QueriesAPIResponse.EvaluationVersionWithMatchingAnswerIdAndReasonValues);
    }

    private static GetNotPerformedReason GetNotPerformedReason => new()
    {
        EvaluationId = 12345,
    };
}