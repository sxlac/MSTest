using System.Reflection;
using Microsoft.Extensions.Configuration;
using Signify.Dps.Test.Utilities.CoreApi.Configs;
using Signify.FOBT.Svc.System.Tests.Core.Constants;
using Signify.QE.Core.Actions;
using Signify.QE.Core.Configs;
using Signify.QE.Core.Models.Provider;
using Signify.Dps.Test.Utilities.Kafka;
using Signify.QE.MSTest.Utilities;

namespace Signify.FOBT.Svc.System.Tests;

[TestClass]
public class StartUp
{
        
    [AssemblyInitialize]
    public static async Task TestStartup(TestContext testContext)
    {
        TestConstants.LoggingHttpMessageHandler = new LoggingHttpMessageHandler(testContext);
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEST_ENV")))
            Environment.SetEnvironmentVariable("TEST_ENV", testContext.Properties["env"]?.ToString());
        var env = Environment.GetEnvironmentVariable("TEST_ENV");
        // Usernames and Passwords will come from the user secrets file when running locally
        var config = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddJsonFile("appsettings-system-test.json", optional: true)
            .Build();
        // LaunchDarklySetup(env, config, testContext);
        DatabaseSetup(env, config, testContext);
        await CoreApiSetup(env, config, testContext);
        await KafkaSetup(env, config, testContext);
        // SmbSetup(env, config, testContext);
    }

    private static async Task KafkaSetup(string env, IConfiguration config, TestContext testContext)
    {
        
        if (env.Equals("prod")) return;
        
        testContext.WriteLine("Setting up Kafka config");
        
        //Setup Kafka consumer Config
        TestConstants.KafkaConfig = new()
        {
            BoostrapServer = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? 
                             config.GetValue<string>($"{env}:kafka:bootstrapServers"),
            GroupId = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID") ?? 
                      config.GetValue<string>($"{env}:kafka:groupID"),
            Topics = ConsumerTopics.ToArray(),
            GetEventRetryCount = 20,
            GetEventRetrySleep = 3000
        };
        
        if (!env.Equals("local"))
        {
            TestConstants.KafkaConfig.SaslUsername = Environment.GetEnvironmentVariable("KAFKA_USERNAME") ?? 
                                                     config.GetValue<string>($"{env}:kafka:username");
            TestConstants.KafkaConfig.SaslPassword = Environment.GetEnvironmentVariable("KAFKA_PASSWORD") ?? 
                                                     config.GetValue<string>($"{env}:kafka:password");
        }

        var producerConfig = new KafkaConfig()
        {
            SaslUsername = Environment.GetEnvironmentVariable("KAFKA_PUB_USERNAME") ?? 
                           config.GetValue<string>($"{env}:kafka:automationPublisher:username"),
            SaslPassword = Environment.GetEnvironmentVariable("KAFKA_PUB_PASSWORD") ?? 
                           config.GetValue<string>($"{env}:kafka:automationPublisher:password"),
            BoostrapServer = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? 
                             config.GetValue<string>($"{env}:kafka:bootstrapServers")
        
        };
        KafkaProducerConfig = KafkaUtils.GetKafkaProducerConfig(env,producerConfig);
        
        var producerHaConfig = new KafkaConfig()
        {
            SaslUsername = Environment.GetEnvironmentVariable("KAFKA_HOME_ACCESS_PUB_USERNAME") ?? 
                           config.GetValue<string>($"{env}:kafka:automationPublisher:homeAccessUsername"),
            SaslPassword = Environment.GetEnvironmentVariable("KAFKA_HOME_ACCESS_PUB_PASSWORD") ?? 
                           config.GetValue<string>($"{env}:kafka:automationPublisher:homeAccessPassword"),
            BoostrapServer = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? 
                             config.GetValue<string>($"{env}:kafka:bootstrapServers")
        };
        KafkaHaProducerConfig = KafkaUtils.GetKafkaProducerConfig(env,producerHaConfig);
        
        await StartKafkaConsumer(env, TestConstants.KafkaConfig, testContext);
    }

    private static void DatabaseSetup(string env, IConfiguration config, TestContext testContext)
    {
        testContext.WriteLine("Setting up Database environment variables");
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FOBT_DB_HOST")))
            Environment.SetEnvironmentVariable("FOBT_DB_HOST", config.GetValue<string>($"{env}:databases:fobt:host"));
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FOBT_DB_USERNAME")))
            Environment.SetEnvironmentVariable("FOBT_DB_USERNAME",
                config.GetValue<string>($"{env}:databases:fobt:username"));
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FOBT_DB_PASSWORD")))
            Environment.SetEnvironmentVariable("FOBT_DB_PASSWORD",
                config.GetValue<string>($"{env}:databases:fobt:password"));
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SERVICE")))
            Environment.SetEnvironmentVariable("SERVICE", config.GetValue<string>($"{env}:databases:fobt:name")); 
        
        testContext.WriteLine("Database environment variables set");
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
        
        testContext.WriteLine("Okta token is set");
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

    private static async Task StartKafkaConsumer(string env,KafkaConfig kafkaConfig, TestContext testContext)
    {
        if (env.Equals("prod")) return;
        testContext.WriteLine("Starting Kafka Consumer");
        testContext.WriteLine($"Subscribing to topics: " + string.Join(", ", kafkaConfig.Topics));
        var consumerConfig = KafkaUtils.GetKafkaConsumerConfig(env, kafkaConfig);
        var kafkaActions = new KafkaActions(kafkaConfig);
        await kafkaActions.CreateAndStartConsumer(consumerConfig);
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
                var dataFactoryActions = new DataFactoryActions();
                var capacityActions = new CapacityActions(TestConstants.CoreApiConfigs.CapacityApiConfig);
                var providerActions = new ProviderActions(TestConstants.CoreApiConfigs.ProviderApiConfig, TestConstants.LoggingHttpMessageHandler)
                    .WithDataFactoryAndCapacity(dataFactoryActions, capacityActions);
                TestConstants.Provider = await providerActions.CreateAndPrepareProvider();
                testContext.WriteLine("Provider created");
                break;
        }
    }
    
}