using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class GetMemberInfoTests
{
    private readonly IMemberInfoApi _memberApi = A.Fake<IMemberInfoApi>();

    private GetMemberInfoHandler CreateSubject() => new(A.Dummy<ILogger<GetMemberInfoHandler>>(), _memberApi);

    [Fact]
    public async Task Handle_WithRequest_QueriesMemberApiWithMemberPlanId()
    {
        const long memberPlanId = 1;

        var request = new GetMemberInfo
        {
            MemberPlanId = memberPlanId
        };

        var expectedMemberInfo = new MemberInfoRs();

        A.CallTo(() => _memberApi.GetMemberByMemberPlanId(A<long>._))
            .Returns(Task.FromResult(expectedMemberInfo));

        var subject = CreateSubject();

        var actualResult = await subject.Handle(request, CancellationToken.None);

        A.CallTo(() => _memberApi.GetMemberByMemberPlanId(A<long>.That.Matches(id => id == memberPlanId)))
            .MustHaveHappenedOnceExactly();

        Assert.Equal(expectedMemberInfo, actualResult);
    }
}