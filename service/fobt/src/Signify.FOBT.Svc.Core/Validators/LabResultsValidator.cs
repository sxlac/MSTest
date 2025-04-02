using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Validators
{
    public class LabResultsValidator : ILabResultsValidator
    {
        /// <inheritdoc />
        public bool AreValidResults(LabResults results)
            => string.IsNullOrWhiteSpace(results.Exception);
    }
}
