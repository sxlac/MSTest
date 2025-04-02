using System;

namespace Signify.Spirometry.Core.Infrastructure
{
    public class ApplicationTime : IApplicationTime
    {
        /// <inheritdoc />
        public DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}
