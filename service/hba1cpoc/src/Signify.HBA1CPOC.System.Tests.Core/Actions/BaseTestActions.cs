using Signify.Dps.Test.Utilities.CoreApi.Actions;
using Signify.Dps.Test.Utilities.Kafka.Actions;
using Signify.HBA1CPOC.System.Tests.Core.Constants;
using static Signify.HBA1CPOC.System.Tests.Core.Constants.TestConstants;

namespace Signify.HBA1CPOC.System.Tests.Core.Actions;

public class BaseTestActions : DatabaseActions
{
    protected readonly CoreApiActions CoreApiActions = new (CoreApiConfigs, Provider.ProviderId, Product, FormVersionId, LoggingHttpMessageHandler);
    public static CoreKafkaActions CoreKafkaActions;
    protected Dictionary<int, string> GeneratePerformedAnswers(string percentA1C=null, string expiryDate=null)
    {
        return new Dictionary<int, string>
        {
            { Answers.Qid91483YesAnswerId, "1" },// Is Test Performed
            { Answers.Qid91491PercentA1CAnswerId, string.IsNullOrEmpty(percentA1C)?"6.9":percentA1C },// A1C Percentage 
            { Answers.Qid91551ExpirationDateAnswerId, string.IsNullOrEmpty(expiryDate)?DateTime.Now.ToString("O"):expiryDate },// Expiration Date
            { Answers.DoSAnswerId, DateTime.Now.ToString("O") }// Date of Service
        };
    }
    
}