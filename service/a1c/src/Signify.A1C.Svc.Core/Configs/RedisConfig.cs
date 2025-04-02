using System.Diagnostics.CodeAnalysis;

namespace Signify.A1C.Svc.Core.Configs
{
    [ExcludeFromCodeCoverage]
    public class RedisConfig
    {
        public string ConnectionString { get; set; }
        public bool CacheEnabled { get; set; }
        public int TotalRetryAttempts { get; set; }
        public int RetryInterval { get; set; } //milliseconds between retry of connection to redis
        public int ObjectLifetime { get; set; } //minutes user can live in cache before replacement
    }
}