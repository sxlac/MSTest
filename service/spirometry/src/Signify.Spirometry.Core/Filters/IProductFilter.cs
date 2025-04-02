using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Events.Akka;
using System.Collections.Generic;

namespace Signify.Spirometry.Core.Filters;

/// <summary>
/// Interface to filter out products to determine if they are applicable to Spirometry
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
    /// Whether any of the supplied products holds are applicable to this process manager
    /// </summary>
    /// <param name="products"></param>
    /// <returns></returns>
    bool ShouldProcess(IEnumerable<ProductHold> products);

    /// <summary>
    /// Whether the supplied product holds is applicable to this process manager
    /// </summary>
    /// <param name="product"></param>
    /// <returns></returns>
    bool ShouldProcess(ProductHold product);

    /// <summary>
    /// Whether any of the supplied product codes are applicable to this process manager
    /// </summary>
    /// <param name="productCodes"></param>
    /// <returns></returns>
    bool ShouldProcess(IEnumerable<string> productCodes);
    
    /// <summary>
    /// Whether or not any of the supplied products are applicable to this Process Manager
    /// </summary>
    /// <param name="products"></param>
    /// <returns></returns>
    bool ShouldProcess(IEnumerable<DpsProduct> products);
    
    /// <summary>
    /// Whether the supplied product code is applicable to this process manager
    /// </summary>
    /// <param name="productCode"></param>
    /// <returns>true/false</returns>
    bool ShouldProcess(string productCode);
}