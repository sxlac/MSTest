using Signify.Spirometry.Core.Models;
using System.Collections.Generic;

namespace Signify.Spirometry.Core.Converters.Frequency
{
    public abstract class OccurrenceFrequencyConverter : IOccurrenceFrequencyConverter
    {
        private readonly IReadOnlyDictionary<int, OccurrenceFrequency> _lookup;

        /// <inheritdoc />
        public int NeverAnswerId { get; }

        /// <inheritdoc />
        public int RarelyAnswerId { get; }

        /// <inheritdoc />
        public int SometimesAnswerId { get; }

        /// <inheritdoc />
        public int OftenAnswerId { get; }

        /// <inheritdoc />
        public int VeryOftenAnswerId { get; }

        protected OccurrenceFrequencyConverter(
            int neverAnswerId,
            int rarelyAnswerId,
            int sometimesAnswerId,
            int oftenAnswerId,
            int veryOftenAnswerId)
        {
            NeverAnswerId = neverAnswerId;
            RarelyAnswerId = rarelyAnswerId;
            SometimesAnswerId = sometimesAnswerId;
            OftenAnswerId = oftenAnswerId;
            VeryOftenAnswerId = veryOftenAnswerId;

            _lookup = new Dictionary<int, OccurrenceFrequency>(5)
            {
                {NeverAnswerId, OccurrenceFrequency.Never},
                {RarelyAnswerId, OccurrenceFrequency.Rarely},
                {SometimesAnswerId, OccurrenceFrequency.Sometimes},
                {OftenAnswerId, OccurrenceFrequency.Often},
                {VeryOftenAnswerId, OccurrenceFrequency.VeryOften}
            };
        }

        /// <inheritdoc />
        public bool TryConvert(int answerId, out OccurrenceFrequency frequency)
        {
            return _lookup.TryGetValue(answerId, out frequency);
        }
    }
}
