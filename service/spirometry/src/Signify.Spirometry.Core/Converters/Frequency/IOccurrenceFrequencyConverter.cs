using Signify.Spirometry.Core.Models;

namespace Signify.Spirometry.Core.Converters.Frequency
{
    /// <summary>
    /// Interface for converting specific evaluation answers to a <see cref="OccurrenceFrequency"/>
    /// </summary>
    public interface IOccurrenceFrequencyConverter
    {
        /// <summary>
        /// AnswerId corresponding to 'Never'
        /// </summary>
        int NeverAnswerId { get; }

        /// <summary>
        /// AnswerId corresponding to 'Rarely'
        /// </summary>
        int RarelyAnswerId { get; }

        /// <summary>
        /// AnswerId corresponding to 'Sometimes'
        /// </summary>
        int SometimesAnswerId { get; }

        /// <summary>
        /// AnswerId corresponding to 'Often'
        /// </summary>
        int OftenAnswerId { get; }

        /// <summary>
        /// AnswerId corresponding to 'Very Often'
        /// </summary>
        int VeryOftenAnswerId { get; }

        /// <summary>
        /// Attempts to convert the supplied <paramref name="answerId"/> to a <see cref="OccurrenceFrequency"/>
        /// </summary>
        /// <param name="answerId"></param>
        /// <param name="frequency"></param>
        /// <returns>False if the given <paramref name="answerId"/> is unknown for this type of converter</returns>
        bool TryConvert(int answerId, out OccurrenceFrequency frequency);
    }
}
