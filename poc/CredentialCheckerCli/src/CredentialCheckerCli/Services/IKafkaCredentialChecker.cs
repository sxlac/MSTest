using Confluent.Kafka;

namespace CredentialCheckerCli;

public interface IKafkaCredentialChecker
{
    Task<List<string>> IsCredentialsValid(AdminClientConfig adminClient);

    AdminClientConfig CreateKafkaCredentials(string bootstrapServers, string saslUsername, string saslPassword);
}