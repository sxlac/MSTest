using Signify.uACR.Core.Events.Akka;
using System.Collections.Generic;

namespace Signify.uACR.Core.Filters;

/// <summary>
/// Interface to filter out products to determine if they are applicable to uACR
/// </summary>
public interface IProductFilter
{
    /// <summary>
    /// Whether or not any of the supplied products are applicable to this process manager
    /// </summary>
    /// <param name="products"></param>
    /// <returns>true/false</returns>
    bool ShouldProcess(IEnumerable<Product> products);

    /// <summary>
    /// Whether or not any of the supplied products are applicable to this Process Manager
    /// </summary>
    /// <param name="products"></param>
    /// <returns>true/false</returns>
    bool ShouldProcess(IEnumerable<DpsProduct> products);

    /// <summary>
    /// Whether or not any of the supplied product codes are applicable to this process manager
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