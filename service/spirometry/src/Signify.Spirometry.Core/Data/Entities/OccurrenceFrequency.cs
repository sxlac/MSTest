using System.Collections.Generic;

namespace Signify.Spirometry.Core.Data.Entities
{
    public class OccurrenceFrequency : IEntityEnum<OccurrenceFrequency>
    {
        public static readonly OccurrenceFrequency Never = new OccurrenceFrequency(1, nameof(Never));
        public static readonly OccurrenceFrequency Rarely = new OccurrenceFrequency(2, nameof(Rarely));
        public static readonly OccurrenceFrequency Sometimes = new OccurrenceFrequency(3, nameof(Sometimes));
        public static readonly OccurrenceFrequency Often = new OccurrenceFrequency(4, nameof(Often));
        public static readonly OccurrenceFrequency VeryOften = new OccurrenceFrequency(5, "Very often");

        /// <inheritdoc />
        public IEnumerable<OccurrenceFrequency> GetAllEnumerations()
            => new[]
            {
                Never,
                Rarely,
                Sometimes,
                Often,
                VeryOften
            };

        /// <summary>
        /// Identity of this record
        /// </summary>
        public short OccurrenceFrequencyId { get; init; }

        /// <summary>
        /// Name of the frequency
        /// </summary>
        public string Frequency { get; }

        private OccurrenceFrequency(short occurrenceFrequencyId, string frequency)
        {
            OccurrenceFrequencyId = occurrenceFrequencyId;
            Frequency = frequency;
        }

        public virtual ICollection<SpirometryExamResult> SpirometryExamResultCoughMucusOccurrenceFrequencies{ get; set; } = new HashSet<SpirometryExamResult>();
        public virtual ICollection<SpirometryExamResult> SpirometryExamResultNoisyChestOccurrenceFrequencies { get; set; } = new HashSet<SpirometryExamResult>();
        public virtual ICollection<SpirometryExamResult> SpirometryExamResultShortnessOfBreathPhysicalActivityOccurrenceFrequencies { get; set; } = new HashSet<SpirometryExamResult>();
    }
}
