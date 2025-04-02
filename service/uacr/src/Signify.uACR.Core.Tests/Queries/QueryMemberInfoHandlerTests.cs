using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.ApiClients.MemberApi;
using Signify.uACR.Core.ApiClients.MemberApi.Responses;
using Signify.uACR.Core.Queries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.uACR.Core.Tests.Queries;

public class QueryMemberInfoHandlerTests
{
    private readonly IMemberApi _memberApi = A.Fake<IMemberApi>();

    private QueryMemberInfoHandler CreateSubject() => new(A.Dummy<ILogger<QueryMemberInfoHandler>>(), _memberApi);

    [Fact]
    public async Task Handle_WithRequest_QueriesMemberApiWithMemberPlanId()
    {
        const long memberPlanId = 1;

        var request = new QueryMemberInfo(memberPlanId);

        var expectedMemberInfo = new MemberInfo();

        A.CallTo(() => _memberApi.GetMemberByMemberPlanId(A<long>._))
            .Returns(Task.FromResult(expectedMemberInfo));

        var subject = CreateSubject();

        var actualResult = await subject.Handle(request, CancellationToken.None);

        A.CallTo(() => _memberApi.GetMemberByMemberPlanId(A<long>.That.Matches(id => id == memberPlanId)))
            .MustHaveHappenedOnceExactly();

        Assert.Equal(expectedMemberInfo, actualResult);
    }
}