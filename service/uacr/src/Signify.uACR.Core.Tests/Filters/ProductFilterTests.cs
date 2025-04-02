using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Filters;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Signify.uACR.Core.Tests.Filters;

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

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithProducts_SharedTests(IEnumerable<string> productCodes, bool expected)
    {
        var products = productCodes.Select(pc => new Product {ProductCode = pc});

        Assert.Equal(expected, new ProductFilter().ShouldProcess(products));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithProductCodes_SharedTests(IEnumerable<string> productCodes, bool expected)
    {
        Assert.Equal(expected, new ProductFilter().ShouldProcess(productCodes));
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
                "UACR"
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
                "UACR"
            },
            true
        ];
        yield return
        [
            new List<string>
            {
                "UACR"
            },
            true
        ];
        yield return
        [
            new List<string>
            {
                "UACR"
            },
            true
        ];
    }
}