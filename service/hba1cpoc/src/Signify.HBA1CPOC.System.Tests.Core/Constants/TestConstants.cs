using Signify.Dps.Test.Utilities.CoreApi.Configs;
using Signify.QE.Core.Models.Provider;
using Signify.QE.MSTest.Utilities;

namespace Signify.HBA1CPOC.System.Tests.Core.Constants;

public static class TestConstants
{
    public static readonly List<string> ConsumerTopics =
    [
        "A1CPOC_Status",
        "A1CPOC_Results",
        "evaluation",
        "cdi_events",
        "rcm_bill"
    ];

    public static CoreApiConfigs CoreApiConfigs { get; set; }
    
    public static LoggingHttpMessageHandler LoggingHttpMessageHandler { get; set; }
    public static Provider Provider { get; set; }

    public const int FormVersionId = 604;
    
    public const string UserName = "QECore";
    
    public const string Application = "Signify.Evaluation.Service";
    
    public const string Product = "HBA1CPOC";
    
    public const string Completed = "Completed";
    
    public const string EvaluationFinalizedEvent = "EvaluationFinalizedEvent";

}