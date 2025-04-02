using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.ApiClients.ProviderApi;
using Signify.uACR.Core.ApiClients.ProviderApi.Responses;
using Signify.uACR.Core.Queries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.uACR.Core.Tests.Queries;

public class QueryProviderInfoHandlerTests
{
    private readonly IProviderApi _providerApi = A.Fake<IProviderApi>();

    private QueryProviderInfoHandler CreateSubject() => new(A.Dummy<ILogger<QueryProviderInfoHandler>>(), _providerApi);

    [Fact]
    public async Task Handle_WithRequest_QueriesProviderApiForProviderId()
    {
        const int providerId = 1;

        var request = new QueryProviderInfo(providerId);

        var expectedResult = new ProviderInfo();

        A.CallTo(() => _providerApi.GetProviderById(A<int>._))
            .Returns(Task.FromResult(expectedResult));

        var subject = CreateSubject();

        var actualResult = await subject.Handle(request, CancellationToken.None);

        A.CallTo(() => _providerApi.GetProviderById(A<int>.That.Matches(id => id == providerId)))
            .MustHaveHappenedOnceExactly();

        Assert.Equal(expectedResult, actualResult);
    }
}