using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using FakeItEasy;
using Refit;
using Signify.uACR.Core.ApiClients.InternalLabResultApi;
using Signify.uACR.Core.ApiClients.InternalLabResultApi.Responses;
using Xunit;

namespace Signify.uACR.Core.Tests.ApiClients.InternalLabResultApi;

public class InternalLabResultApiTests
{
    private readonly IInternalLabResultApi _internalLabResultApi = A.Fake<IInternalLabResultApi>();

    [Fact]
    public void InternalLabResultApi_GetLabResultByLabResultId_Run()
    {
        // Arrange
        var apiResponseBody = A.Fake<GetResultResponse>();
        apiResponseBody.LabResultId = RandomNumberGenerator.GetInt32(0, int.MaxValue);
        var apiResponse = new ApiResponse<GetResultResponse>(new HttpResponseMessage(HttpStatusCode.Accepted),
            apiResponseBody, null!);

        // Act
        A.CallTo(() => _internalLabResultApi.GetLabResultByLabResultId(A<string>._))
            .Returns(apiResponse);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, apiResponse.StatusCode);
    }
}