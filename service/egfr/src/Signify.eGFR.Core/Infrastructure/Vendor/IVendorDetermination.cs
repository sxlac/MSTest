namespace Signify.eGFR.Core.Infrastructure.Vendor;

public interface IVendorDetermination
{
    VendorDetermination.Vendor GetVendorFromBarcode(string barcode);
}