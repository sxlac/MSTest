using System;
using System.Collections.Generic;
using System.Linq;
using Signify.PAD.Svc.Core.Events;

namespace Signify.PAD.Svc.Core.Filters;

public class ProductFilter : IProductFilter
{
    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<Product> products)
        => products != null && products.Any(each => IsPad(each?.ProductCode));

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<DpsProduct> products)
        => products != null && products.Any(each => IsPad(each?.ProductCode));

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<string> productCodes)
        => productCodes != null && productCodes.Any(IsPad);

    /// <inheritdoc />
    public bool ShouldProcess(string productCode)
    {
        return !string.IsNullOrWhiteSpace(productCode) && IsPad(productCode);
    }

    /// <summary>
    /// Checks if the Process Manager product code matches (case insensitive) the one supplied
    /// </summary>
    /// <param name="productCode"></param>
    /// <returns></returns>
    private static bool IsPad(string productCode)
        => Constants.Application.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase);
}