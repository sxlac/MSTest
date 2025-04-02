using Signify.eGFR.Core.Exceptions;
using Xunit;

namespace Signify.eGFR.Core.Tests.Exceptions;

public class DuplicateBarcodeExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties_Test()
    {
        const long evaluationId = 1;
        const string barcode = "1234-abcd";

        var ex = new DuplicateBarcodeException(evaluationId, barcode);

        Assert.Equal(evaluationId, ex.EvaluationId);
        Assert.Equal(barcode, ex.Barcode);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const long evaluationId = 1;
        const string barcode = "1234-abcd";

        var ex = new DuplicateBarcodeException(evaluationId, barcode);

        Assert.Equal($"EvaluationId :{evaluationId} with Barcode:{barcode} already exists in DB", ex.Message);
    }
}