using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class LabResultNotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const long evaluationId = 1;

        var ex = new LabResultNotFoundException(evaluationId);

        Assert.Equal(evaluationId, ex.EvaluationId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const long evaluationId = 1;

        var ex = new LabResultNotFoundException(evaluationId);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal("LabResult with EvaluationId:1 not found in DB", ex.Message);
    }
}