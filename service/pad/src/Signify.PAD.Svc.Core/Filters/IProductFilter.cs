using System.Collections.Generic;
using Signify.PAD.Svc.Core.Events;

namespace Signify.PAD.Svc.Core.Filters;

/// <summary>
/// Interface to filter out products to determine if they are applicable to this Process Manager
/// </summary>
public interface IProductFilter
{
    /// <summary>
    /// Whether any of the supplied products are applicable to this process manager
    /// </summary>
    /// <param name="products"></param>
    /// <returns>true/false</returns>
    bool ShouldProcess(IEnumerable<Product> products);

    /// <summary>
    /// Whether any of the supplied products are applicable to this Process Manager
    /// </summary>
    /// <param name="products"></param>
    /// <returns>true/false</returns>
    bool ShouldProcess(IEnumerable<DpsProduct> products);

    /// <summary>
    /// Whether any of the supplied product codes are applicable to this process manager
    /// </summary>
    /// <param name="productCodes"></param>
    /// <returns>true/false</returns>
    bool ShouldProcess(IEnumerable<string> productCodes);


    /// <summary>
    /// Whether the supplied product code is applicable to this process manager
    /// </summary>
    /// <param name="productCode"></param>
    /// <returns>true/false</returns>
    bool ShouldProcess(string productCode);
}