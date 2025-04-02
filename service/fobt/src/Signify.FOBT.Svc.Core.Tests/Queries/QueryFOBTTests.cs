using System.Threading.Tasks;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class QueryFOBTTests : IClassFixture<MockDbFixture>
{
    private readonly QueryFOBTHandler _handler;

    public QueryFOBTTests(MockDbFixture mockDbFixture)
    {
        _handler = new QueryFOBTHandler(mockDbFixture.Context);
    }

    [Fact]
    public async Task QueryFOBTHandler_GetByBarcode()
    {
        // Arrange
        var request = new QueryFOBT
        {
            Barcode = "01234567891234"
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.Equal(result.FOBT.Barcode, request.Barcode);
        Assert.Equal(QueryFOBTStatus.BarcodeExists, result.Status);
    }

    [Fact]
    public async Task QueryFOBTHandler_GetByAppointmentId()
    {
        // Arrange
        var request = new QueryFOBT
        {
            AppointmentId = 1000084715,
            Barcode = "tetete"
        };
        

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.Equal(result.FOBT.AppointmentId, request.AppointmentId);
        Assert.Equal(324356, result.FOBT.EvaluationId);
    }

    [Fact]
    public async Task QueryFOBTHandler_CouldNotFindExam()
    {
        // Arrange
        var request = new QueryFOBT
        {
            AppointmentId = 123456,
            Barcode = "tetete"
        };
        

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.Null(result.FOBT);
        Assert.Equal(QueryFOBTStatus.NotFound, result.Status);
    }
}