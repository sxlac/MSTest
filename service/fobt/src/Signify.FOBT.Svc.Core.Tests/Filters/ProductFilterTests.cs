using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Filters;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Filters;

public class ProductFilterTests
{
    [Fact]
    public void ShouldProcess_WithProducts_NullCollection_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess((IEnumerable<Product>)null));
    }

    [Fact]
    public void ShouldProcess_WithProductCodes_NullCollection_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess((IEnumerable<string>)null));
    }

    [Fact]
    public void ShouldProcess_WithNullProduct_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess(new List<Product> { null }));
    }

    [Fact]
    public void ShouldProcess_WithProductCode_Empty_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcessBilling(null));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithProducts_SharedTests(IEnumerable<string> productCodes, bool expected)
    {
        var products = productCodes?.Select(pc => new Product { ProductCode = pc });

        Assert.Equal(expected, new ProductFilter().ShouldProcess(products));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithProductCodes_SharedTests(IEnumerable<string> productCodes, bool expected)
    {
        Assert.Equal(expected, new ProductFilter().ShouldProcess(productCodes));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_Billing_TestData))]
    public void ShouldProcess_WithProductCodeString_SharedTests(string productCode, bool expected)
    {
        Assert.Equal(expected, new ProductFilter().ShouldProcessBilling(productCode));
    }

    public static IEnumerable<object[]> ProductCodes_TestData()
    {
        yield return
        [
            new List<string>
            {
                "some other product code",
                null,
                string.Empty,
                "?!.",
                "FOBT"
            },
            true
        ];
        yield return
        [
            new List<string>
            {
                "some other product code",
                "yet another product code"
            },
            false
        ];
        yield return
        [
            new List<string>
            {
                "fobt"
            },
            true
        ];
        yield return
        [
            new List<string>
            {
                "Fobt"
            },
            true
        ];
        yield return
        [
            new List<string>
            {
                "FoBt"
            },
            true
        ];
        yield return
        [
            null,
            false
        ];
    }

    public static IEnumerable<object[]> ProductCodes_Billing_TestData()
    {
        yield return
        [
            "some other product code", false
        ];
        yield return
        [
            "", false
        ];
        yield return
        [
            " ", false
        ];
        yield return
        [
            "?!.", false
        ];
        yield return
        [
            null, false
        ];
        yield return
        [
            "Fobt", false
        ];
        yield return
        [
            "FOBT", false
        ];
        yield return
        [
            ApplicationConstants.BILLING_PRODUCT_CODE_LEFT, true
        ];
        yield return
        [
            ApplicationConstants.BILLING_PRODUCT_CODE_LEFT.ToLower(), true
        ];
        yield return
        [
            ApplicationConstants.BILLING_PRODUCT_CODE_LEFT.ToUpper(), true
        ];
        yield return
        [
            ApplicationConstants.BILLING_PRODUCT_CODE_RESULTS, true
        ];
        yield return
        [
            ApplicationConstants.BILLING_PRODUCT_CODE_RESULTS.ToLower(), true
        ];
        yield return
        [
            ApplicationConstants.BILLING_PRODUCT_CODE_RESULTS.ToUpper(), true
        ];
    }
}