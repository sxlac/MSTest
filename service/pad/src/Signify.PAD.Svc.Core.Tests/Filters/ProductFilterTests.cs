using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Filters;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Filters;

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
        Assert.False(new ProductFilter().ShouldProcess((string)null));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithProducts_SharedTests(IEnumerable<string> productCodes, bool expected)
    {
        var products = productCodes.Select(pc => new Product { ProductCode = pc });

        Assert.Equal(expected, new ProductFilter().ShouldProcess(products));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithProductCodes_SharedTests(IEnumerable<string> productCodes, bool expected)
    {
        Assert.Equal(expected, new ProductFilter().ShouldProcess(productCodes));
    }

    [Fact]
    public void ShouldProcess_WithNullDpsProduct_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess(new List<DpsProduct> { null }));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithDpsProducts_SharedTests(IEnumerable<string> productCodes, bool expected)
    {
        var products = productCodes.Select(pc => new DpsProduct { ProductCode = pc });

        Assert.Equal(expected, new ProductFilter().ShouldProcess(products));
    }

    [Theory]
    [InlineData("some other product code", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("?!.", false)]
    [InlineData("pad", true)]
    [InlineData("PAD", true)]
    [InlineData("PaD", true)]
    [InlineData("paD", true)]
    [InlineData("pAD", true)]
    [InlineData("PAd", true)]
    [InlineData("Pad", true)]
    public void ShouldProcess_WithProductCodeString_SharedTests(string productCode, bool expected)
    {
        Assert.Equal(expected, new ProductFilter().ShouldProcess(productCode));
    }

    public static IEnumerable<object[]> ProductCodes_TestData()
    {
        yield return new object[]
        {
            new List<string>
            {
                "some other product code",
                null,
                string.Empty,
                "?!.",
                "PAD"
            },
            true
        };
        yield return new object[]
        {
            new List<string>
            {
                "some other product code",
                "yet another product code"
            },
            false
        };
        yield return new object[]
        {
            new List<string>
            {
                null
            },
            false
        };
        yield return new object[]
        {
            new List<string>
            {
                "pad"
            },
            true
        };
        yield return new object[]
        {
            new List<string>
            {
                "Pad"
            },
            true
        };
        yield return new object[]
        {
            new List<string>
            {
                "PaD"
            },
            true
        };
    }
}