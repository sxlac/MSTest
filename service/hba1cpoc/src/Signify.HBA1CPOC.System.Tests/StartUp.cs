using System.Reflection;
using Microsoft.Extensions.Configuration;
using Signify.Dps.Test.Utilities.CoreApi.Configs;
using Signify.Dps.Test.Utilities.Kafka;
using Signify.Dps.Test.Utilities.Kafka.Actions;
using Signify.HBA1CPOC.System.Tests.Core.Constants;
using Signify.QE.Core.Actions;
using Signify.QE.Core.Configs;
using Signify.QE.Core.Models.Provider;
using Signify.QE.MSTest.Utilities;

namespace Signify.Dps.HBA1CPOC.System.Tests;

[TestClass]
public class StartUp
{
    
    [AssemblyInitialize]
    public static async Task TestStartup(TestContext testContext)
    {
        TestConstants.LoggingHttpMessageHandler = new LoggingHttpMessageHandler(testContext);
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEST_ENV")))
            Environment.SetEnvironmentVariable("TEST_ENV", testContext.Properties["env"]!.ToString());
        var env = Environment.GetEnvironmentVariable("TEST_ENV");
        // Usernames and Passwords will come from the user secrets file when running locally
        var config = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddJsonFile("appsettings-system-test.json", optional: true)
            .Build();
        //LaunchDarklySetup(env, config, testContext);
        DatabaseConfig(env, config, testContext);
        await KafkaSetup(env, config, testContext);
        await CoreApiSetup(env, config, testContext);
        await CreateProvider(env, testContext);
    }
    
    private static void LaunchDarklySetup(string env, IConfigurationRoot config, TestContext testContext)
    {
        if (env.Equals("local")) return;
        LaunchDarklyConfig.SdkKey = Environment.GetEnvironmentVariable("LD_SDK_KEY") ??
                                    config.GetValue<string>($"{env}:launchDarkly:sdkKey");
        LaunchDarklyConfig.UserKey = Environment.GetEnvironmentVariable("LD_USER_KEY") ??
                                     config.GetValue<string>($"{env}:launchDarkly:userKey");
        LaunchDarklyConfig.UserName = Environment.GetEnvironmentVariable("LD_USERNAME") ??
                                      config.GetValue<string>($"{env}:launchDarkly:userName");
    }
    
    private static async Task CoreApiSetup(string env, IConfiguration config, TestContext testContext)
    {
        testContext.WriteLine("Setting up Okta config");
        OktaConfig oktaConfig = new()
        {
            Username = Environment.GetEnvironmentVariable("OKTA_USERNAME") ??
                       config.GetValue<string>($"{env}:okta:username"),
            Password = Environment.GetEnvironmentVariable("OKTA_PASSWORD") ??
                       config.GetValue<string>($"{env}:okta:password"),
            Url = Environment.GetEnvironmentVariable("OKTA_URL") ??
                  testContext.Properties["Okta.Url"]?.ToString(),
            RedirectUrl = Environment.GetEnvironmentVariable("OKTA_REDIRECT_URL") ??
                          testContext.Properties["Okta.RedirectUrl"]?.ToString(),
            Scope = Environment.GetEnvironmentVariable("OKTA_SCOPE") ??
                    testContext.Properties["Okta.Scope"]?.ToString(),
            ClientId = Environment.GetEnvironmentVariable("OKTA_CLIENT_ID") ??
                       testContext.Properties["Okta.ClientId"]?.ToString()
        };

        TestConstants.CoreApiConfigs = await GetCoreApiConfigs(env, oktaConfig, testContext);
        
        await CreateProvider(env, testContext);
        
        testContext.WriteLine("Okta setup and Provider creation complete");
    }

    private static async Task<CoreApiConfigs> GetCoreApiConfigs(string env, OktaConfig oktaConfig, TestContext testContext)
    {
        var oktaToken = await GetOktaToken(env, oktaConfig);
        var coreApiConfigFactory = new CoreApiConfigFactory();
        return new CoreApiConfigs
        {
            AppointmentApiConfig = coreApiConfigFactory.GetConfig<AppointmentApiConfig>(testContext.Properties["AppointmentApiUrl"]!.ToString(), oktaToken),
            AvailabilityApiConfig = coreApiConfigFactory.GetConfig<AvailabilityApiConfig>(testContext.Properties["AvailabilityApiUrl"]!.ToString(), oktaToken),
            EvaluationApiConfig = coreApiConfigFactory.GetConfig<EvaluationApiConfig>(testContext.Properties["EvaluationApiUrl"]!.ToString(), oktaToken),
            ProviderApiConfig = coreApiConfigFactory.GetConfig<ProviderApiConfig>(testContext.Properties["ProviderApiUrl"]!.ToString(), oktaToken),
            MemberApiConfig = coreApiConfigFactory.GetConfig<MemberApiConfig>(testContext.Properties["MemberApiUrl"]!.ToString(), oktaToken),
            CapacityApiConfig = coreApiConfigFactory.GetConfig<CapacityApiConfig>(testContext.Properties["CapacityApiUrl"]!.ToString(), oktaToken),
            DataFactoryApiConfig = coreApiConfigFactory.GetConfig<DataFactoryApiConfig>(testContext.Properties["DataFactoryApiUrl"]!.ToString(), oktaToken)
        };
    }

    private static async Task<string> GetOktaToken(string env, OktaConfig oktaConfig)
    {
        if (env.Equals("local")) return "";
        
        var oktaActions = new OktaActions(oktaConfig);
        return await oktaActions.GetAccessToken();
    }
    
    private static async Task KafkaSetup(string env, IConfiguration config, TestContext testContext)
    {
        if (env.Equals("prod")) return;
        
        testContext.WriteLine("Setting up Kafka config");
        
        //Setup Kafka consumer Config
        var kafkaConfig = new KafkaConfig
        {
            BootstrapServer = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? 
                             config.GetValue<string>($"{env}:kafka:bootstrapServers"),
            GroupId = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID") ?? 
                      config.GetValue<string>($"{env}:kafka:groupID"),
            Topics = TestConstants.ConsumerTopics.ToArray(),
            GetEventRetryCount = 20,
            GetEventRetrySleep = 3000
        };
        
        if (!env.Equals("local"))
        {
            kafkaConfig.SaslUsername = Environment.GetEnvironmentVariable("KAFKA_USERNAME") ?? 
                                                 config.GetValue<string>($"{env}:kafka:username");
            kafkaConfig.SaslPassword = Environment.GetEnvironmentVariable("KAFKA_PASSWORD") ?? 
                                       config.GetValue<string>($"{env}:kafka:password");
        }

        var producerConfig = new KafkaConfig
        {
            SaslUsername = Environment.GetEnvironmentVariable("KAFKA_PUB_USERNAME") ?? 
                           config.GetValue<string>($"{env}:kafka:automationPublisher:username"),
            SaslPassword = Environment.GetEnvironmentVariable("KAFKA_PUB_PASSWORD") ?? 
                           config.GetValue<string>($"{env}:kafka:automationPublisher:password"),
            BootstrapServer = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? 
                             config.GetValue<string>($"{env}:kafka:bootstrapServers")
        };
        
        var kafkaProducerConfig = KafkaUtils.GetKafkaProducerConfig(env,producerConfig);
        
        BaseTestActions.CoreKafkaActions = new CoreKafkaActions(kafkaConfig, kafkaProducerConfig);
        
        await StartKafkaConsumer(env, kafkaConfig, testContext);

    }
    
    private static void DatabaseConfig(string env, IConfiguration config, TestContext testContext)
    {
        testContext.WriteLine("Setting up Database environment variables.");
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HBA1CPOC_DB_HOST")))
            Environment.SetEnvironmentVariable("HBA1CPOC_DB_HOST", config.GetValue<string>($"{env}:databases:hba1cpoc:host"));
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HBA1CPOC_DB_USERNAME")))
            Environment.SetEnvironmentVariable("HBA1CPOC_DB_USERNAME", config.GetValue<string>($"{env}:databases:hba1cpoc:username"));
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HBA1CPOC_DB_PASSWORD")))
            Environment.SetEnvironmentVariable("HBA1CPOC_DB_PASSWORD", config.GetValue<string>($"{env}:databases:hba1cpoc:password"));
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SERVICE")))
            Environment.SetEnvironmentVariable("SERVICE", config.GetValue<string>($"{env}:databases:hba1cpoc:name"));
        testContext.WriteLine("Database environment variables set.");
    }
    
    
    private static async Task StartKafkaConsumer(string env,KafkaConfig kafkaConfig, TestContext testContext)
    {
        if (env.Equals("prod")) return;
        testContext.WriteLine("Starting Kafka Consumer");
        testContext.WriteLine($"Subscribing to topics: " + string.Join(", ", kafkaConfig.Topics));
        var consumerConfig = KafkaUtils.GetKafkaConsumerConfig(env, kafkaConfig);
        await BaseTestActions.CoreKafkaActions.CreateAndStartConsumer(consumerConfig);
    }
    
    private static async Task CreateProvider(string env, TestContext testContext)
    {
        switch (env)
        {
            case "local":
                TestConstants.Provider = new Provider
                {
                    ProviderId = 42879,
                    NationalProviderIdentifier = "9230239051",
                    FirstName = "John",
                    LastName = "Doe"
                };
                break;
            case "prod":
                TestConstants.Provider = new Provider
                {
                    ProviderId = 26985,
                    NationalProviderIdentifier = "8441956355",
                    FirstName = "Test89167",
                    LastName = "Test89167"
                };
                break;
            default:
                var dataFactoryActions = new DataFactoryActions(TestConstants.CoreApiConfigs.DataFactoryApiConfig);
                var capacityActions = new CapacityActions(TestConstants.CoreApiConfigs.CapacityApiConfig);
                var providerActions = new ProviderActions(TestConstants.CoreApiConfigs.ProviderApiConfig, TestConstants.LoggingHttpMessageHandler)
                    .WithDataFactoryAndCapacity(dataFactoryActions, capacityActions);
                TestConstants.Provider = await providerActions.CreateAndPrepareProvider();
                testContext.WriteLine("Provider created.");
                break;
        }
    }

}