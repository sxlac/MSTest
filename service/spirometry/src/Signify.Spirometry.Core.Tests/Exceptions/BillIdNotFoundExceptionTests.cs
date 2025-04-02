using Signify.Spirometry.Core.Exceptions;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Exceptions;

public class BillIdNotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const long evaluationId = 1;
        var billId = Guid.NewGuid();

        var ex = new BillNotFoundException(evaluationId, billId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(billId, ex.RcmBillId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const long evaluationId = 1;
        var billId = Guid.Parse("5a5ae438-fd89-43b7-8736-3c7e0dee86d8");

        var ex = new BillNotFoundException(evaluationId, billId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(billId, ex.RcmBillId);
        Assert.Equal("Unable to find a bill with EvaluationId=1, for RCMBillId=5a5ae438-fd89-43b7-8736-3c7e0dee86d8", ex.Message);
    }
}