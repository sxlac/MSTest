using System;
using Signify.CKD.Svc.Core.Infrastructure;

namespace Signify.CKD.Svc.Core.Tests
{
    /// <summary>
    /// Fake application time that always returns the same timestamp for <see cref="UtcNow"/>
    /// </summary>
    public class FakeApplicationTime : IApplicationTime
    {
        private readonly DateTime _now = new DateTime(2022, 2, 3, 4, 5, 6, DateTimeKind.Utc);

        /// <remarks>
        /// Always returns the same value
        /// </remarks>
        public DateTime UtcNow()
        {
            return _now;
        }
    }
}
