using Signify.QE.Core.Models.Provider;
using Signify.QE.MSTest.Utilities;
using Signify.Dps.Test.Utilities.CoreApi.Configs;

namespace Signify.Spirometry.Svc.System.Tests.Core.Constants;


public static class TestConstants
{
    public static readonly List<string> ConsumerTopics =
    [
        "spirometry_status",
        "spirometry_result",
        "evaluation",
        "providerpay_internal",
        "cdi_events",
        "rcm_bill"
    ];
    public const string Product = "SPIROMETRY";
    public const string ApplicationId = "Signify.Evaluation.Service";
    public static Provider Provider { get; set; }
    public static CoreApiConfigs CoreApiConfigs { get; set; }
    public static LoggingHttpMessageHandler LoggingHttpMessageHandler { get; set; }
    public const int FormVersionId = 604;
}