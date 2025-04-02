using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Signify.Tools.MessageQueue.Helpers.Types;
using Signify.Tools.MessageQueue.Services.Interfaces;
using Signify.Tools.MessageQueue.Settings;

namespace Signify.Tools.MessageQueue.Services
{
    public sealed class NServiceBusService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ISendMessageManager _sendMessageManager;
        private readonly ITemplateFileService _templateFileService;
        private readonly NServiceBusSettings _settings;

        public NServiceBusService
        (
            ILogger<NServiceBusService> logger,
            ISendMessageManager sendMessageManager,
            ITemplateFileService templateFileService,
            IOptions<NServiceBusSettings> options
        )
        {
            _logger = logger;
            _sendMessageManager = sendMessageManager;
            _templateFileService = templateFileService;
            _settings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Started running the NServiceBus Queue Tool");

                var processManagerType = Enum.Parse<ProcessManagerType>(_settings.ProcessManager);
                var eventMessage = _settings.EventMessage;
                var actionType = Enum.Parse<ActionType>(_settings.ActionType);

                // switch to action setup in appsettings file
                switch (actionType)
                {
                    case ActionType.SendMessage:
                        {
                            await _sendMessageManager.SendMessagesToQueue(processManagerType, eventMessage, stoppingToken);
                            break;
                        }
                    case ActionType.GenerateTemplateFile:
                        {
                            await _templateFileService.Generate(processManagerType, eventMessage, stoppingToken);
                            break;
                        }
                    default:
                        {
                            _logger.LogInformation("No action taken due to action type entered.  ActionType:{ActionType}", actionType);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception Info when running NServiceBus Tool");
            }
            finally
            {
                _logger.LogInformation("Completed running the NServiceBus Queue Tool");
            }
        }
    }
}
