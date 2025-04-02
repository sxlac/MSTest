using CommandLine;
using Confluent.Kafka;
using CredentialCheckerCli.Utilities;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CredentialCheckerCli.Commands;

[Verb("kafkaCredentials", HelpText = "Checks the correct access to topics & valid credentials for Kafka")]

public class CheckKafkaCredentialsCommand : ICommand
{
    [Option("bootstrapServer", Required = true, HelpText = "The name of the Kafka boostrap server")]
    public string bootstrapServer { get; set; } = string.Empty;
    [Option("username", HelpText = "The Kafka username")]
    public string Username { get; set; } = string.Empty;
    [Option("password", HelpText = "The password for the Kafka credentials")]
    public string Password { get; set; } = string.Empty;

    [Option("topics", HelpText = "A list of the topics you expect the credentials to have access to.")]
    public IEnumerable<string?> Topics { get; set; }

    public async Task ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        var _adminClient = new AdminClientConfig()
        {
            BootstrapServers = bootstrapServer,
            SaslUsername = Username,
            SaslPassword = Password,
            SaslMechanism = SaslMechanism.Plain,
            SecurityProtocol = SecurityProtocol.SaslSsl,
        };
        
        KafkaValidator _kafkaValidator = new KafkaValidator();
        
        ValidationResult validationResult = _kafkaValidator.Validate(_adminClient);

        if (validationResult.IsValid)
        {
            var _checker = services.GetRequiredService<IKafkaCredentialChecker>();

            var adminClient = _checker.CreateKafkaCredentials(bootstrapServer,Username, Password);

            var credentialOutcome = await _checker.IsCredentialsValid(adminClient);

            if (credentialOutcome == null)
            {
                throw new Exception("The credentials you provided are invalid.");
            }

            var remainingTopics = Topics.Except(credentialOutcome);
            
            var isTopicsReturned = Topics.All(str => credentialOutcome.Any(word => word == str));
            
            if (!isTopicsReturned)
            {
                Console.WriteLine("The Kafka credentials do not have access to the following topics:");
                foreach (string topic in remainingTopics)
                {
                    Console.WriteLine(topic);
                }
                throw new Exception("The Kafka credentials do not have access to certain topics.");
            }
            Console.WriteLine($"Kafka credentials for server ({bootstrapServer}) are valid, and return the following list of topics:");
            foreach (string topic in credentialOutcome)
            {
                Console.WriteLine(topic);
            }
        }
        else
        {
            throw new Exception($"{validationResult}");
        }
    }
}