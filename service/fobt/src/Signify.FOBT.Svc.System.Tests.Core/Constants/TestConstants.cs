using Confluent.Kafka;
using Signify.QE.Core.Models.Provider;
using Signify.QE.Core.Configs;
using Signify.QE.MSTest.Utilities;
using Signify.Dps.Test.Utilities.CoreApi.Configs;

namespace Signify.FOBT.Svc.System.Tests.Core.Constants;


public static class TestConstants
{
    public static readonly List<string> ConsumerTopics = new List<string>(){
        "FOBT_Status",
        "FOBT_Results",
        "evaluation",
        "providerpay_internal",
        "cdi_events",
        "rcm_bill"
    };

    public const string Product = "FOBT";
    public const string StatusTopic = "FOBT_Status";
    public const string ResultsTopic = "FOBT_Results";
    public const string ApplicationId = "Signify.Evaluation.Service";

    public static ProducerConfig KafkaProducerConfig { get; set; }
    public static ProducerConfig KafkaHaProducerConfig { get; set; }
    public static KafkaConfig KafkaConfig { get; set; }
    public static Provider Provider { get; set; }
    public static CoreApiConfigs CoreApiConfigs { get; set; }
    public static LoggingHttpMessageHandler LoggingHttpMessageHandler { get; set; }

    public const int FormVersionId = 604;
}