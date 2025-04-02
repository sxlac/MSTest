using System.Collections.Generic;
using System.Linq;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Filters;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Filters;

public class ProductFilterTests
{
    [Fact]
    public void ShouldProcess_WithProducts_NullCollection_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess((IEnumerable<Product>)null));
    }

    [Fact]
    public void ShouldProcess_WithDpsProducts_NullCollection_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess((IEnumerable<DpsProduct>)null));
    }

    [Fact]
    public void ShouldProcess_WithNullProductList_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess(new List<Product> { null }));
    }
    
    [Fact]
    public void ShouldProcess_WithNullProduct_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess(""));
    }

    [Fact]
    public void ShouldProcess_WithNullDpsProduct_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess(new List<DpsProduct> { null }));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithProducts_Tests(IEnumerable<string> productCodes, bool expected)
    {
        var products = productCodes.Select(pc => new Product(pc));

        Assert.Equal(expected, new ProductFilter().ShouldProcess(products));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithDpsProducts_Tests(IEnumerable<string> productCodes, bool expected)
    {
        var products = productCodes.Select(pc => new DpsProduct { ProductCode = pc });

        Assert.Equal(expected, new ProductFilter().ShouldProcess(products));
    }

    [Theory]
    [MemberData(nameof(ProductCodes_TestData))]
    public void ShouldProcess_WithProductCodes_Tests(IEnumerable<string> productCodes, bool expected)
    {
        Assert.Equal(expected, new ProductFilter().ShouldProcess(productCodes));
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
                "DEE"
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
                "dee"
            },
            true
        };
        yield return new object[]
        {
            new List<string>
            {
                "Dee"
            },
            true
        };
        yield return new object[]
        {
            new List<string>
            {
                "deE"
            },
            true
        };
        yield return new object[]
        {
            new List<string>
            {
                "dEe"
            },
            true
        };
    }
}