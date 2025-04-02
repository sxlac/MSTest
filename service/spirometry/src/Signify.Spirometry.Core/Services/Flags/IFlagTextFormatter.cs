using Signify.Spirometry.Core.Data.Entities;

namespace Signify.Spirometry.Core.Services.Flags
{
    /// <summary>
    /// Formatter for generating flag text to be displayed in the IHE mobile app in a clarification
    /// to the provider that performed a spirometry evaluation that had a clinically valid
    /// overread 
    /// </summary>
    public interface IFlagTextFormatter
    {
        /// <summary>
        /// Formats the flag text based on the given result
        /// </summary>
        string FormatFlagText(SpirometryExamResult result);
    }
}
