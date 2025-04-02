using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Mocks.Json.Queries;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Queries;

public class GetNotPerformedReasonTest : IClassFixture<Utilities.MockDbFixture>
{
    private readonly IEvaluationApi _evalApi;
    private readonly GetNotPerformedReasonHandler _subject;

    public GetNotPerformedReasonTest(Utilities.MockDbFixture mockDbFixture)
    {
        _evalApi = A.Fake<IEvaluationApi>();
        var logger = A.Fake<ILogger<GetNotPerformedReasonHandler>>();
        _subject = new GetNotPerformedReasonHandler(_evalApi, mockDbFixture.Context, logger);
    }

    /// <summary>
    /// Response type
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Get_Matching_Not_Performed_Reason()
    {
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponse());
        var actualResult = await _subject.Handle(getNotPerformedReason, CancellationToken.None);
        actualResult.Should().Be(null);
    }

    [Fact]
    public async Task Handle_EvaluationReasonCapture_ReturnsAnswerValue()
    {
        // Arrange
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponseWithMatchingAnswerId());

        // Act
        var actualResult = await _subject.Handle(getNotPerformedReason, CancellationToken.None);

        // Assert
        actualResult.NotPerformedReason.AnswerId.Should().Be(NotPerformedReason.MemberRecentlyCompleted.AnswerId);
    }

    [Fact]
    public async Task Handle_EvaluationWithoutReasonAnswer_ReturnsNull()
    {
        // Arrange
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponseWithoutMatchingAnswerId());

        // Act
        var actualResult = await _subject.Handle(getNotPerformedReason, CancellationToken.None);

        // Assert
        actualResult.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EvaluationReason_CaptureReasonAndReasonNoteAndReasonTypeValues()
    {
        // Arrange
        A.CallTo(() => _evalApi.GetEvaluationVersion(A<int>._, A<string>._)).Returns(GetApiResponseWithReasonForMissingTest());

        // Act
        var actualResult = await _subject.Handle(getNotPerformedReason, CancellationToken.None);

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
        return JsonConvert.DeserializeObject<EvaluationVersionRs>(QueriesAPIResponse.EvaluationVersion);
    }

    private static EvaluationVersionRs GetApiResponseWithReasonForMissingTest()
    {
        return JsonConvert.DeserializeObject<EvaluationVersionRs>(QueriesAPIResponse.EvaluationVersionWithMatchingAnswerIdAndReasonValues);
    }


    /// <summary>
    /// Provides the mock response
    /// </summary>
    /// <returns></returns>
    /// <summary>
    /// Provides the mock response
    /// </summary>
    /// <returns></returns>
    private static EvaluationVersionRs GetApiResponse()
    {
        return JsonConvert.DeserializeObject<EvaluationVersionRs>(QueriesAPIResponse.EvaluationVersion);
    }

    private static GetNotPerformedReason getNotPerformedReason => new()
    {
        EvaluationId = 12345,
    };
}