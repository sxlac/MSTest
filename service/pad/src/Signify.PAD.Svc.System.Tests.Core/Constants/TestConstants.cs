using Confluent.Kafka;
using Signify.Dps.Test.Utilities.CoreApi.Actions;
using Signify.Dps.Test.Utilities.CoreApi.Configs;
using Signify.Dps.Test.Utilities.Kafka.Actions;
using Signify.QE.Core.Configs;
using Signify.QE.Core.Models.Provider;
using Signify.QE.Core.Utilities.LaunchDarkly;
using Signify.QE.MSTest.Utilities;

namespace Signify.PAD.Svc.System.Tests.Core.Constants;

public static class TestConstants
{
    public static readonly List<string> ConsumerTopics =
    [
        "PAD_Status",
        "PAD_Results",
        "evaluation",
        "providerpay_internal",
        "cdi_events",
        "rcm_bill",
        "pad_clinical_support"
    ];

    public const string Product = "PAD";
    public const string StatusTopic = "PAD_Status";
    public const string ResultsTopic = "PAD_Results";
    public const string ClinicalTopic = "pad_clinical_support";
    public const string ApplicationId = "Signify.Evaluation.Service";
    public static LaunchDarklyConfig LaunchDarklyConfig { get; set; }
    public static Provider Provider { get; set; }
    
    public static ProducerConfig KafkaProducerConfig { get; set; }
    
    public static KafkaConfig KafkaConfig { get; set; }
    public static CoreApiConfigs CoreApiConfigs { get; set; }
    public static LoggingHttpMessageHandler LoggingHttpMessageHandler { get; set; }

    public const int FormVersionId = 604;

}