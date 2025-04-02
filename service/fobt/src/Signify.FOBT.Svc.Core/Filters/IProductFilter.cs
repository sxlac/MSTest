using Signify.FOBT.Svc.Core.Events;
using System.Collections.Generic;

namespace Signify.FOBT.Svc.Core.Filters;

/// <summary>
/// Interface to filter out products to determine if they are applicable to FOBT
/// </summary>
public interface IProductFilter
{
    /// <summary>
    /// Whether or not any of the supplied products are applicable to this process manager
    /// </summary>
    /// <returns>true/false</returns>
    bool ShouldProcess(IEnumerable<Product> products);

    /// <summary>
    /// Whether or not any of the supplied products are applicable to this process manager
    /// </summary>
    /// <returns>true/false</returns>
    bool ShouldProcess(IEnumerable<string> productCodes);

    /// <summary>
    /// Whether or not any of the supplied products are applicable to this Process Manager
    /// </summary>
    /// <param name="products"></param>
    /// <returns>true/false</returns>
    bool ShouldProcess(IEnumerable<DpsProduct> products);

    /// <summary>
    /// Whether the supplied product code is applicable to this process manager
    /// </summary>
    /// <param name="productCode"></param>
    /// <returns>true/false</returns>
    bool ShouldProcessBilling(string productCode);
}