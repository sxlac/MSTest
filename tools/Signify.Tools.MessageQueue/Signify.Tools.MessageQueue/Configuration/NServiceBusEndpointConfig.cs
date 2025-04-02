using NServiceBus;

namespace Signify.Tools.MessageQueue.Configuration
{
    public static class NServiceBusEndpointConfig
    {
        public static EndpointConfiguration Setup(string queueName, string connectionString, int concurrencyLimit)
        {
            var endpointConfiguration = new EndpointConfiguration(queueName);
            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
            var messageProcessingConcurrencyLimit = concurrencyLimit;
            endpointConfiguration.LimitMessageProcessingConcurrencyTo(messageProcessingConcurrencyLimit);

            var azureServiceBusConnectionString = connectionString;
            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transport.ConnectionString(azureServiceBusConnectionString);

            endpointConfiguration.SendFailedMessagesTo($"{queueName}.error");
            endpointConfiguration.DisableFeature<NServiceBus.Features.Sagas>();
            endpointConfiguration.EnableInstallers();

            return endpointConfiguration;
        }
    }
}
