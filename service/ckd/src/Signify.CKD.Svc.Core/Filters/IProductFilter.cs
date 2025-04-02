using System.Collections.Generic;
using Signify.CKD.Svc.Core.Events;

namespace Signify.CKD.Svc.Core.Filters;

/// <summary>
/// Interface to filter out products to determine if they are applicable to this Process Manager
/// </summary>
public interface IProductFilter
{
    /// <summary>
    /// Whether or not any of the supplied products are applicable to this process manager
    /// </summary>
    /// <param name="products"></param>
    /// <returns></returns>
    bool ShouldProcess(IEnumerable<Product> products);

    /// <summary>
    /// Whether or not any of the supplied products are applicable to this Process Manager
    /// </summary>
    /// <param name="products"></param>
    /// <returns></returns>
    bool ShouldProcess(IEnumerable<DpsProduct> products);

    /// <summary>
    /// Whether any of the supplied product codes are applicable to this process manager
    /// </summary>
    /// <param name="productCodes"></param>
    /// <returns></returns>
    bool ShouldProcess(IEnumerable<string> productCodes);
}
