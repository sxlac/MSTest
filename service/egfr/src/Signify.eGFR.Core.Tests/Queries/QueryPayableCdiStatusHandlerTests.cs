using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.Queries;

public class QueryPayableCdiStatusHandlerTests : IClassFixture<MockDbFixture>
{
    private readonly ILogger<QueryPayableCdiStatusHandler> _logger = A.Dummy<ILogger<QueryPayableCdiStatusHandler>>();
    private readonly DataContext _dataContext;

    public QueryPayableCdiStatusHandlerTests(MockDbFixture mockDbFixture)
    {
        _dataContext = mockDbFixture.SharedDbContext;
    }

    [Theory]
    [InlineData(1)]
    public async Task Handle_WhenPayableCdi_DoesNot_Exist_Returns_Null(long evaluationId)
    {
        var subject = new QueryPayableCdiStatusHandler(_logger, _dataContext);
        MockDbFixture.PopulateFakeData(_dataContext);
        
        var actual = await subject.Handle(new QueryPayableCdiStatus(evaluationId), default);

        Assert.Null(actual);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Handle_WhenPayableCdi_Exist_Returns_Entity(long evaluationId)
    {
        var subject = new QueryPayableCdiStatusHandler(_logger, _dataContext);
        MockDbFixture.PopulateFakeData(_dataContext);

        var actual = await subject.Handle(new QueryPayableCdiStatus(evaluationId), default);

        Assert.NotNull(actual);
    }
}