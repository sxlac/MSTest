using Confluent.Kafka;

namespace CredentialCheckerCli;

public class KafkaCredentialChecker : IKafkaCredentialChecker
{
    public AdminClientConfig CreateKafkaCredentials(string bootstrapServers, string saslUsername, string saslPassword)
    {
        var _adminClient = new AdminClientConfig();
        _adminClient.BootstrapServers = bootstrapServers;
        _adminClient.SaslUsername = saslUsername;
        _adminClient.SaslPassword = saslPassword;
        _adminClient.SaslMechanism = SaslMechanism.Plain;
        _adminClient.SecurityProtocol = SecurityProtocol.SaslSsl;

        return _adminClient;
    }

    public async Task<List<string>> IsCredentialsValid(AdminClientConfig adminClient)
    {
        using (var builder = new AdminClientBuilder(adminClient).Build())
        {
            try
            {
                var metadata = builder.GetMetadata(TimeSpan.FromSeconds(10));
                var topicsMetadata = metadata.Topics;
                var topicNames = metadata.Topics.Select(a => a.Topic).ToList();
                return topicNames;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occured while trying to verify Kafka credentials {ex}");
                throw;
            }
        }
    }
}