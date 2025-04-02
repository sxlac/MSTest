using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signify.FOBT.Svc.Core.Filters
{
    public class ProductFilter : IProductFilter
    {
        /// <inheritdoc />
        public bool ShouldProcess(IEnumerable<Product> products)
            => products != null && products.Any(each => IsFobt(each?.ProductCode));

        /// <inheritdoc />
        public bool ShouldProcess(IEnumerable<string> productCodes)
            => productCodes != null && productCodes.Any(each => IsFobt(each));

        /// <inheritdoc />
        public bool ShouldProcess(IEnumerable<DpsProduct> products)
            => products != null && products.Any(each => IsFobt(each?.ProductCode));

        /// <inheritdoc />
        public bool ShouldProcessBilling(string productCode)
        {
            return !string.IsNullOrWhiteSpace(productCode) && IsFobt(productCode, true);
        }

        /// <summary>
        /// Checks if the Process Manager product code matches (case insensitive) the one supplied
        /// </summary>
        /// <param name="productCode"></param>
        /// <param name="isBillAccepted">whether the product code match is for <see cref="BillRequestAccepted"/> event</param>
        /// <returns></returns>
        private static bool IsFobt(string productCode, bool isBillAccepted = false)
        {
            if (!isBillAccepted)
            {
                return ApplicationConstants.PRODUCT_CODE.Equals(productCode, StringComparison.OrdinalIgnoreCase);
            }

            return ApplicationConstants.BILLING_PRODUCT_CODE_LEFT.Equals(productCode, StringComparison.OrdinalIgnoreCase)
                   || ApplicationConstants.BILLING_PRODUCT_CODE_RESULTS.Equals(productCode, StringComparison.OrdinalIgnoreCase);
        }
    }
}