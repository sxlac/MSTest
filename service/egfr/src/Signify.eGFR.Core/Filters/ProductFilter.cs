using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Events.Akka;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signify.eGFR.Core.Filters;

public class ProductFilter : IProductFilter
{
    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<Product> products)
    {
        return products != null && products.Any(each => IsEgfr(each?.ProductCode));
    }

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<DpsProduct> products)
        => products != null && products.Any(each => IsEgfr(each?.ProductCode));

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<string> productCodes)
    {
        return productCodes != null && productCodes.Any(IsEgfr);
    }

    /// <inheritdoc />
    public bool ShouldProcess(string productCode)
    {
        return !string.IsNullOrWhiteSpace(productCode) && IsEgfr(productCode);
    }

    /// <summary>
    /// Checks if the Process Manager product code matches (case insensitive) the one supplied
    /// </summary>
    /// <param name="productCode"></param>
    /// <returns></returns>
    private static bool IsEgfr(string productCode) =>
        ProductCodes.eGFR.Equals(productCode, StringComparison.OrdinalIgnoreCase);
}