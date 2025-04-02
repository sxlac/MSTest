using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class GetProviderInfoTest
{
    private readonly IProviderApi _providerApi = A.Fake<IProviderApi>();

    private GetProviderInfoHandler CreateSubject()
        => new(A.Dummy<ILogger<GetProviderInfoHandler>>(), _providerApi);

    [Fact]
    public async Task Handle_WithRequest_QueriesProviderApiForProviderId()
    {
        const int providerId = 1;

        var request = new GetProviderInfo
        {
            ProviderId = providerId
        };

        var expectedResult = new ProviderRs();

        A.CallTo(() => _providerApi.GetProviderById(A<int>._))
            .Returns(Task.FromResult(expectedResult));

        var subject = CreateSubject();

        var actualResult = await subject.Handle(request, CancellationToken.None);

        A.CallTo(() => _providerApi.GetProviderById(A<int>.That.Matches(id => id == providerId)))
            .MustHaveHappenedOnceExactly();

        Assert.Equal(expectedResult, actualResult);
    }
}