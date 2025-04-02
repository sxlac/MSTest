using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Validators
{
    public interface ILabResultsValidator
    {
        /// <summary>
        /// Checks whether the the given lab results are valid
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        bool AreValidResults(LabResults results);
    }
}
