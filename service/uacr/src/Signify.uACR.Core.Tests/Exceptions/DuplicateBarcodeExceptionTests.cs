using Signify.uACR.Core.Exceptions;
using Xunit;

namespace Signify.uACR.Core.Tests.Exceptions;

public class DuplicateBarcodeExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const long evaluationId = 1;
        const string barcode = "abc123";

        var ex = new DuplicateBarcodeException(evaluationId, barcode);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(barcode, ex.Barcode);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const long evaluationId = 1;
        const string barcode = "abc123";

        var ex = new DuplicateBarcodeException(evaluationId, barcode);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(barcode, ex.Barcode);
        Assert.Equal("EvaluationId :1 with Barcode:abc123 already exists in DB", ex.Message);
    }
}