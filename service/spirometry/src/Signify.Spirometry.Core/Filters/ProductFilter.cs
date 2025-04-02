using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Events.Akka;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signify.Spirometry.Core.Filters;

public class ProductFilter : IProductFilter
{
    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<Product> products)
    {
        return products != null && products.Any(each => IsSpirometry(each?.ProductCode));
    }

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<ProductHold> products)
    {
        return products != null && products.Any(ShouldProcess);
    }

    /// <inheritdoc />
    public bool ShouldProcess(ProductHold product)
    {
        return product != null && IsSpirometry(product.Code);
    }

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<string> productCodes)
    {
        return productCodes != null && productCodes.Any(IsSpirometry);
    }
    
    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<DpsProduct> products)
        => products != null && products.Any(each => IsSpirometry(each?.ProductCode));

    public bool ShouldProcess(string productCode)
        => IsSpirometry(productCode);
    
    private static bool IsSpirometry(string productCode) =>
        ProductCodes.Spirometry.Equals(productCode, StringComparison.OrdinalIgnoreCase);
}