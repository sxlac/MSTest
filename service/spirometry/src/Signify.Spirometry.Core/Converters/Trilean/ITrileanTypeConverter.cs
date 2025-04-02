using Signify.Spirometry.Core.Models;

namespace Signify.Spirometry.Core.Converters.Trilean
{
    /// <summary>
    /// Interface for converting specific evaluation answers to a <see cref="TrileanType"/>
    /// </summary>
    public interface ITrileanTypeConverter
    {
        /// <summary>
        /// AnswerId corresponding to 'Unknown'
        /// </summary>
        int UnknownAnswerId { get; }

        /// <summary>
        /// AnswerId corresponding to 'Yes'
        /// </summary>
        int YesAnswerId { get; }

        /// <summary>
        /// AnswerId corresponding to 'No'
        /// </summary>
        int NoAnswerId { get; }

        /// <summary>
        /// Attempts to convert the supplied <see cref="answerId"/> to a <see cref="TrileanType"/>
        /// </summary>
        /// <param name="answerId"></param>
        /// <param name="trileanType"></param>
        /// <returns>False if the given <see cref="answerId"/> is unknown for this type of converter</returns>
        bool TryConvert(int answerId, out TrileanType trileanType);
    }
}
