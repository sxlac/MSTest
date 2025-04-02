using Signify.Dps.Test.Utilities.CoreApi.Configs;
using Signify.QE.Core.Models.Provider;
using Signify.QE.Core.Utilities.LaunchDarkly;
using Signify.QE.MSTest.Utilities;

namespace Signify.DEE.Svc.System.Tests.Core.Constants;

public static class TestConstants
{
    public static readonly List<string> ConsumerTopics =
    [
        "dee_status",
        "dee_results",
        "evaluation",
        "providerpay_internal",
        "cdi_events",
        "rcm_bill"
    ];
    
    public const string Product = "DEE";
    
    public static LaunchDarklyConfig LaunchDarklyConfig { get; set; }
    public static Provider Provider { get; set; }
    public static CoreApiConfigs CoreApiConfigs { get; set; }
    public static LoggingHttpMessageHandler LoggingHttpMessageHandler { get; set; }
    
    public const int FormVersionId = 705;

}