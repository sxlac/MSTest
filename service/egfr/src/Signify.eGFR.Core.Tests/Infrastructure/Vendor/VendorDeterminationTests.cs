using System.Collections.Generic;
using Signify.eGFR.Core.Infrastructure.Vendor;
using Xunit;

namespace Signify.eGFR.Core.Tests.Infrastructure.Vendor;

public class VendorDeterminationTests
{
    private static VendorDetermination CreateSubject()
        => new();
    
    [Theory]
    [MemberData(nameof(VendorList))]
    public void Should_Throw_when_Inputs_Invalid(string vendor, VendorDetermination.Vendor expected)
    {
        var subject = CreateSubject();
        Assert.Equal(subject.GetVendorFromBarcode(vendor), expected);
    }

    public static IEnumerable<object[]> VendorList()
    {
        yield return ["", VendorDetermination.Vendor.NotDefined];
        yield return [null, VendorDetermination.Vendor.NotDefined];
        yield return ["abc", VendorDetermination.Vendor.NotDefined];
        yield return ["123", VendorDetermination.Vendor.NotDefined];
        yield return ["@£$%^", VendorDetermination.Vendor.NotDefined];
        yield return ["LGC-", VendorDetermination.Vendor.LetsGetChecked];
        yield return ["lgc", VendorDetermination.Vendor.LetsGetChecked];
        yield return ["lgC", VendorDetermination.Vendor.LetsGetChecked];
        yield return ["lGc", VendorDetermination.Vendor.LetsGetChecked];
        yield return ["lGC", VendorDetermination.Vendor.LetsGetChecked];
        yield return ["Lgc", VendorDetermination.Vendor.LetsGetChecked];
        yield return ["LGc", VendorDetermination.Vendor.LetsGetChecked];
        yield return ["LGC", VendorDetermination.Vendor.LetsGetChecked];
    }
}