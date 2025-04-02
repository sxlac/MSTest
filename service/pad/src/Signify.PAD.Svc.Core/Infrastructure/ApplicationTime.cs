using System;

namespace Signify.PAD.Svc.Core.Infrastructure
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
