using Signify.Spirometry.Core.Models;
using System.Collections.Generic;

namespace Signify.Spirometry.Core.Converters.Trilean
{
    public abstract class TrileanTypeConverter : ITrileanTypeConverter
    {
        private readonly IReadOnlyDictionary<int, TrileanType> _lookup;

        /// <inheritdoc />
        public int UnknownAnswerId { get; }

        /// <inheritdoc />
        public int YesAnswerId { get; }

        /// <inheritdoc />
        public int NoAnswerId { get; }

        /// <exception cref="System.ArgumentException">Thrown if any two or more supplied answer ids are equal</exception>
        protected TrileanTypeConverter(int unknownAnswerId, int yesAnswerId, int noAnswerId)
        {
            UnknownAnswerId = unknownAnswerId;
            YesAnswerId = yesAnswerId;
            NoAnswerId = noAnswerId;

            _lookup = new Dictionary<int, TrileanType>(3)
            {
                {UnknownAnswerId, TrileanType.Unknown},
                {YesAnswerId, TrileanType.Yes},
                {NoAnswerId, TrileanType.No}
            };
        }

        /// <inheritdoc />
        public bool TryConvert(int answerId, out TrileanType trileanType)
        {
            return _lookup.TryGetValue(answerId, out trileanType);
        }
    }
}
