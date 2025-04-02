using Signify.uACR.Core.Exceptions;
using Xunit;

namespace Signify.uACR.Core.Tests.Exceptions;

public class KedProductNotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const long evaluationId = 1;
        const decimal uacrResult = 59.0m;
        const bool isBillable = true;
        
        var ex = new KedProductNotFoundException(evaluationId, uacrResult, isBillable);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(uacrResult, ex.UacrResult);
        Assert.Equal(isBillable, ex.IsBillable);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const long evaluationId = 1;
        const decimal uacrResult = 59.0m;
        const bool isBillable = true;
        
        var ex = new KedProductNotFoundException(evaluationId, uacrResult, isBillable);
        
        Assert.Equal("KED missing product code not found exception EvaluationId=1, Result=59.0, IsBillable=True", ex.Message);
    }
}