using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NServiceBus;
using Signify.Tools.MessageQueue.Configuration;
using Signify.Tools.MessageQueue.Core.Interfaces;
using Signify.Tools.MessageQueue.Helpers.Types;
using Signify.Tools.MessageQueue.Queue.Interfaces;
using Signify.Tools.MessageQueue.Settings;

namespace Signify.Tools.MessageQueue.Queue
{
    public class MessengerService : IMessengerService
    {
        private readonly ILogger<MessengerService> _logger;
        private readonly IMessagesCsvFileReader _messagesCsvFileReader;
        private readonly NServiceBusSettings _serviceSettings;
        private readonly ConnectionStringSettings _connectionStringSettings;

        public MessengerService
        (
            ILogger<MessengerService> logger,
            IMessagesCsvFileReader messagesCsvFileReader,
            IOptions<NServiceBusSettings> serviceOptions,
            IOptions<ConnectionStringSettings> connectionStringOptions
        )
        {
            _logger = logger;
            _messagesCsvFileReader = messagesCsvFileReader;
            _serviceSettings = serviceOptions.Value;
            _connectionStringSettings = connectionStringOptions.Value;
        }

        public async Task SendMessages<T>(ProcessManagerType processManagerType, string eventMessage, CancellationToken cancellation)
        {
            _logger.LogInformation("Entered SendMessages");

            var queueName = GetConfiguredQueueName(processManagerType);
            var concurrencyLimit = GetConfiguredConcurrencyLimit(processManagerType);
            var endpointConfiguration = NServiceBusEndpointConfig.Setup(queueName, _connectionStringSettings.AzureServiceBusConnectionString, concurrencyLimit);

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

            var fileInfo = new FileInfo(_serviceSettings.InputFileLocationAndName);
            var messages = await _messagesCsvFileReader.ReadMessageValues<T>(fileInfo, cancellation);
            foreach (var message in messages)
            {
                await endpointInstance.SendLocal(message);
            }

            await endpointInstance.Stop().ConfigureAwait(false);
            _logger.LogInformation("Completed SendMessages");
        }

        private string GetConfiguredQueueName(ProcessManagerType processManagerType)
        {
            switch (processManagerType)
            {
                case ProcessManagerType.CKD:
                    {
                        return _serviceSettings.CkdSettings!.QueueName;
                    }
                case ProcessManagerType.DEE:
                    {
                        return _serviceSettings.DeeSettings!.QueueName;
                    }
                case ProcessManagerType.EGFR:
                    {
                        return _serviceSettings.EgfrSettings!.QueueName;
                    }
                case ProcessManagerType.FOBT:
                    {
                        return _serviceSettings.FobtSettings!.QueueName;
                    }
                case ProcessManagerType.HBA1CPOC:
                    {
                        return _serviceSettings.HbA1cPocSettings!.QueueName;
                    }
                case ProcessManagerType.HBA1C:
                    {
                        return _serviceSettings.HbA1CSettings!.QueueName;
                    }
                case ProcessManagerType.PAD:
                    {
                        return _serviceSettings.PadSettings!.QueueName;
                    }
                case ProcessManagerType.Spirometry:
                    {
                        return _serviceSettings.SpirometrySettings!.QueueName;
                    }
                default:
                    {
                        return string.Empty;
                    }
            }
        }

        private int GetConfiguredConcurrencyLimit(ProcessManagerType processManagerType)
        {
            switch (processManagerType)
            {
                case ProcessManagerType.CKD:
                    {
                        return _serviceSettings.CkdSettings!.ConcurrencyLimit;
                    }
                case ProcessManagerType.DEE:
                    {
                        return _serviceSettings.DeeSettings!.ConcurrencyLimit;
                    }
                case ProcessManagerType.EGFR:
                    {
                        return _serviceSettings.EgfrSettings!.ConcurrencyLimit;
                    }
                case ProcessManagerType.FOBT:
                    {
                        return _serviceSettings.FobtSettings!.ConcurrencyLimit;
                    }
                case ProcessManagerType.HBA1CPOC:
                    {
                        return _serviceSettings.HbA1cPocSettings!.ConcurrencyLimit;
                    }
                case ProcessManagerType.HBA1C:
                    {
                        return _serviceSettings.HbA1CSettings!.ConcurrencyLimit;
                    }
                case ProcessManagerType.PAD:
                    {
                        return _serviceSettings.PadSettings!.ConcurrencyLimit;
                    }
                case ProcessManagerType.Spirometry:
                    {
                        return _serviceSettings.SpirometrySettings!.ConcurrencyLimit;
                    }
                default:
                    {
                        return 4;
                    }
            }
        }
    }
}
