namespace Signify.uACR.Core.Infrastructure.Vendor;

public interface IVendorDetermination
{
    VendorDetermination.Vendor GetVendor(string barcode);
}