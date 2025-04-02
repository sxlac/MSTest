using Signify.Dps.Test.Utilities.CoreApi.Configs;
using Signify.QE.Core.Models.Provider;
using Signify.QE.Core.Utilities.LaunchDarkly;
using Signify.QE.MSTest.Utilities;

namespace Signify.uACR.System.Tests.Core.Constants;

public static class TestConstants
{
    public static readonly List<string> ConsumerTopics =
    [
        "uacr_status",
        "uacr_results",
        "evaluation",
        "dps_oms_order",
        "dps_labresult_uacr",
        "cdi_events",
        "rcm_bill",
        "dps_labs_webhook_inbound"
    ];
    public const string Product = "UACR";
    public static LaunchDarklyConfig LaunchDarklyConfig { get; set; }
    public static Provider Provider { get; set; }
    public static CoreApiConfigs CoreApiConfigs { get; set; }
    public static LoggingHttpMessageHandler LoggingHttpMessageHandler { get; set; }
    public const int FormVersionId = 717;
    public const string StatusTopic = "uacr_status";
    public const string Vendor = "LetsGetChecked";
    public const string ResultsTopic = "uacr_results";
    public const string BarCodePattern = "L*G*C*-1234-1234-1234";
    public const string AlphaCodePattern = "ABCDEF";
    public const string InvalidBarCodePattern = "L*G*C*-1O34-1234-1234";
    public const string InvalidAlphaCodePattern = "ABCDE1";
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