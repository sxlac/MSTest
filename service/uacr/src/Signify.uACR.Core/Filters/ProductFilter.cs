using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Events.Akka;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signify.uACR.Core.Filters;

public class ProductFilter : IProductFilter
{
    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<Product> products) =>
        products != null && products.Any(each => IsuAcr(each?.ProductCode));

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<DpsProduct> products)
        => products != null && products.Any(each => IsuAcr(each?.ProductCode));

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<string> productCodes) => productCodes != null && productCodes.Any(IsuAcr);

    /// <inheritdoc />
    public bool ShouldProcess(string productCode)
        => !string.IsNullOrWhiteSpace(productCode) && IsuAcr(productCode);

    private static bool IsuAcr(string productCode) =>
        Application.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase);
}