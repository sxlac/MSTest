using System.Diagnostics.CodeAnalysis;

namespace Signify.CKD.Svc.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public class SerilogConfig
    {
        public Properties Properties { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Properties
    {

        public string Environment { get; set; }
        public string App { get; set; }
        public string Version { get; set; }
    }
}