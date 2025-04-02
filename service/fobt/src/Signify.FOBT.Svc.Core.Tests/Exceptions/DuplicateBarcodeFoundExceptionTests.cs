using Signify.FOBT.Svc.Core.Exceptions;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Exceptions;

public class DuplicateBarcodeFoundExceptionTests
{
    [Fact]
    public void Constructor_BarcodeFound_SetsProperties()
    {
        const string barcode = "someBarcode";

        var ex = new DuplicateBarcodeFoundException(barcode);

        Assert.Equal(barcode, ex.Barcode);
        Assert.Equal("Duplicate Barcode found, for Barcode " + barcode, ex.Message);
    }
}