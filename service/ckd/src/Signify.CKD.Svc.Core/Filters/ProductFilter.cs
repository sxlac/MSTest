using System;
using System.Collections.Generic;
using System.Linq;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Events;

namespace Signify.CKD.Svc.Core.Filters;

public class ProductFilter : IProductFilter
{
    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<Product> products)
        => products != null && products.Any(each => IsCkd(each?.ProductCode));

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<DpsProduct> products)
        => products != null && products.Any(each => IsCkd(each?.ProductCode));

    /// <inheritdoc />
    public bool ShouldProcess(IEnumerable<string> productCodes)
        => productCodes != null && productCodes.Any(IsCkd);

    private static bool IsCkd(string productCode)
        => Application.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase);
}
