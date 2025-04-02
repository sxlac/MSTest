using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signify.DEE.Svc.Core.Filters;

public class ProductFilter : IProductFilter
{
    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<Product> products)
        => products != null && products.Any(each => IsDee(each?.ProductCode));

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<DpsProduct> products)
        => products != null && products.Any(each => IsDee(each?.ProductCode));

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<ProductHold> products)
    {
        return products != null && products.Any(ShouldProcess);
    }

    /// <inheritdoc />
    public bool ShouldProcess(ProductHold product)
    {
        return product != null && IsDee(product.Code);
    }

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<string> productCodes)
        => productCodes != null && productCodes.Any(IsDee);

    /// <inheritdoc />
    public bool ShouldProcess(string productCode)
        => !string.IsNullOrWhiteSpace(productCode) && IsDee(productCode);

    private static bool IsDee(string productCode)
        => ApplicationConstants.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase);
}