using Signify.DEE.Svc.Core.Exceptions;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Exceptions;

public class UnmatchedOrderExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        const string localId = "1";
        const int irisPatientId = 2;

        var ex = new UnmatchedOrderException(localId, irisPatientId);

        Assert.Equal(localId, ex.LocalId);
        Assert.Equal(irisPatientId, ex.IrisPatientId);
    }

    [Fact]
    public void Constructor_SetsMessage()
    {
        const string localId = "1";
        const int irisPatientId = 2;

        var ex = new UnmatchedOrderException(localId, irisPatientId);

        Assert.Equal(localId, ex.LocalId);
        Assert.Equal(irisPatientId, ex.IrisPatientId);
        Assert.Equal($"Unable to match iris order (Iris Patient ID: {irisPatientId} with local Id {localId}", ex.Message);
    }
}