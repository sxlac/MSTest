
using System;

namespace Signify.eGFR.Core.Infrastructure.Vendor;

public class VendorDetermination : IVendorDetermination
{
    public Vendor GetVendorFromBarcode(string barcode)
    {
        if (barcode != null && barcode.StartsWith(Constants.Vendor.LgcBarcodePrefix, StringComparison.InvariantCultureIgnoreCase))
            return Vendor.LetsGetChecked;

        return Vendor.NotDefined;
    }

    public enum Vendor
    {
        LetsGetChecked,
        NotDefined
    }
}