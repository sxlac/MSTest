using Confluent.Kafka;
using FluentValidation;

namespace CredentialCheckerCli.Utilities;

public class KafkaValidator : AbstractValidator<AdminClientConfig>
{
    public KafkaValidator()
    {
        RuleFor(adminClient => adminClient.BootstrapServers).NotEmpty().WithMessage("bootstrap server field is empty, please add a value.");
        RuleFor(adminClient => adminClient.SaslUsername).NotEmpty().WithMessage("username field is empty, please add a value.");
        RuleFor(adminClient => adminClient.SaslPassword).NotEmpty().WithMessage("password field is empty, please add a value.");
    }
}