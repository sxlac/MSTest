using Signify.DEE.Svc.Core.Exceptions;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Exceptions;

public class UnmatchedVendorImageExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        const string vendorImageLocalId = "1";
        const int examLocalId = 2;

        var ex = new UnmatchedVendorImageException(vendorImageLocalId, examLocalId);

        Assert.Equal(vendorImageLocalId, ex.VendorImageLocalId);
        Assert.Equal(examLocalId, ex.ExamLocalId);
    }

    [Fact]
    public void Constructor_SetsMessage_Test()
    {
        const string vendorImageLocalId = "1";
        const int examLocalId = 2;

        var ex = new UnmatchedVendorImageException(vendorImageLocalId, examLocalId);

        Assert.Equal(vendorImageLocalId, ex.VendorImageLocalId);
        Assert.Equal(examLocalId, ex.ExamLocalId);
        Assert.Equal($"Received an image with VendorImageLocalId {vendorImageLocalId} that does not belong to our exam {examLocalId}", ex.Message);
    }
}