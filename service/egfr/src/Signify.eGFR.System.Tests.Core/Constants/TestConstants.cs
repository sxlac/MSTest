using Confluent.Kafka;
using Signify.QE.Core.Models.Provider;
using Signify.QE.Core.Configs;
using Signify.QE.MSTest.Utilities;
using Signify.Dps.Test.Utilities.CoreApi.Configs;

namespace Signify.eGFR.System.Tests.Core.Constants;

public static class TestConstants
{
    public static readonly List<string> ConsumerTopics =
    [
        "egfr",
        "egfr_lab_results",
        "egfr_results",
        "egfr_status",
        "evaluation",
        "providerpay_internal",
        "cdi_events",
        "rcm_bill",
        "dps_labresult_egfr",
        "dps_oms_order",
        "dps_labs_webhook_inbound"
    ];

    public const string Product = "EGFR";
    public const string StatusTopic = "egfr_status";
    public const string ResultsTopic = "egfr_results";
    public const string ApplicationId = "Signify.Evaluation.Service";
    public const string LgcBarcodePattern  = "L*G*C*-1234-5678-9876";
    public const string InvalidLgcBarcodePattern  = "G*L*C*-1234-5678-9876";
    public const string AlphaBarcodePattern  = "ABCDEF";
    public const string Notes = "ABCDEF";
    public static ProducerConfig KafkaProducerConfig { get; set; }
    public static ProducerConfig KafkaHaProducerConfig { get; set; }
    public static KafkaConfig KafkaConfig { get; set; }
    public static CoreApiConfigs CoreApiConfigs { get; set; }
    public static LoggingHttpMessageHandler LoggingHttpMessageHandler { get; set; }
    public static Provider Provider { get; set; }
    public const int FormVersionId = 717;
    public const string WiremockLocalBaseUrl = "http://localhost:9090";
    public static string WiremockUrl { get; set; }
    public const string WebhookTopic = "dps_rms_labresult";
    public const string LgcVendorName = "LGC";
    public const string ValidExamType = "KED";
    public const string MockVendorAuthUrl = "/LabsWebhookPm/oauth2/token";
    public const string MockReportUrl = "//LabsWebhookPm/DiagnosticReport";
    public static readonly Dictionary<int,int> ReasonAnswerIdMappings = new()
    {
        {Answers.KedMemberRecentlyCompletedAnswerId, Answers.MemberRecentlyCompletedAnswerId},
        {Answers.KedScheduledToCompleteAnswerId, Answers.ScheduledToCompleteAnswerId},
        {Answers.KedInsufficientTrainingAnswerId, Answers.InsufficientTrainingAnswerId},
        {Answers.KedMemberApprehensionAnswerId, Answers.MemberApprehensionAnswerId},
        {Answers.KedNotInterestedAnswerId, Answers.NotInterestedAnswerId},
        {Answers.KedNoSuppliesOrEquipmentAnswerId, Answers.NoSuppliesOrEquipmentAnswerId},
        {Answers.KedEnvironmentalIssueAnswerId, Answers.EnvironmentalIssueAnswerId},
        {Answers.KedTechnicalIssueAnswerId, Answers.TechnicalIssueAnswerId},
        {Answers.KedMemberPhysicallyUnableAnswerId, Answers.MemberPhysicallyUnableAnswerId},
        
    };
}