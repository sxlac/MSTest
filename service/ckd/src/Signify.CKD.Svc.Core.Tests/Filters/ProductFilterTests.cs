using System.Collections.Generic;
using System.Linq;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Filters;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Filters;

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
    public void ShouldProcess_WithNullProduct_ReturnsFalse()
    {
        Assert.False(new ProductFilter().ShouldProcess(new List<Product> { null }));
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
                "CKD"
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
                "ckd"
            },
            true
        };
        yield return new object[]
        {
            new List<string>
            {
                "Ckd"
            },
            true
        };
        yield return new object[]
        {
            new List<string>
            {
                "CkD"
            },
            true
        };
    }
}