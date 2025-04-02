using Signify.Spirometry.Core.Events.Akka;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Filters;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Filters;

public class ProductFilterTests
{
    [Fact]
    public void ShouldProcess_WithProducts_NullCollection_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess((IEnumerable<Product>)null));
    }

    [Fact]
    public void ShouldProcess_WithProductHolds_NullCollection_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess((IEnumerable<ProductHold>)null));
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
    public void ShouldProcess_WithNullProductHold_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess(new List<ProductHold> { null }));
        Assert.False(new ProductFilter().ShouldProcess((ProductHold) null));
    }
        
    [Fact]
    public void ShouldProcess_WithNullDpsProductHold_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess(new List<DpsProduct> { null }));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithProducts_SharedTests(IEnumerable<string> productCodes, bool expected)
    {
        var products = productCodes.Select(pc => new Product {ProductCode = pc});

        Assert.Equal(expected, new ProductFilter().ShouldProcess(products));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithProductHolds_SharedTests(IEnumerable<string> productCodes, bool expected)
    {
        var products = productCodes.Select(pc => new ProductHold {Code = pc});

        Assert.Equal(expected, new ProductFilter().ShouldProcess(products));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithProductCodes_SharedTests(IEnumerable<string> productCodes, bool expected)
    {
        Assert.Equal(expected, new ProductFilter().ShouldProcess(productCodes));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithDpsProductCodes_SharedTests(IEnumerable<string> productCodes, bool expected)
    {
        var products = productCodes.Select(pc => new DpsProduct {ProductCode = pc});

        Assert.Equal(expected, new ProductFilter().ShouldProcess(products));
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
                "SPIROMETRY"
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
                null
            },
            false
        ];
        yield return
        [
            new List<string>
            {
                "spirometry"
            },
            true
        ];
        yield return
        [
            new List<string>
            {
                "Spirometry"
            },
            true
        ];
        yield return
        [
            new List<string>
            {
                "spiroMeTRY"
            },
            true
        ];
    }
}