using Microsoft.Extensions.Logging;
using Signify.Tools.MessageQueue.Helpers.Extensions;
using Signify.Tools.MessageQueue.Helpers.Types;
using Signify.Tools.MessageQueue.Queue.Interfaces;
using Signify.Tools.MessageQueue.Services.Interfaces;

namespace Signify.Tools.MessageQueue.Services
{
    public class SendMessageManager : ISendMessageManager
    {
        private readonly ILogger<SendMessageManager> _logger;
        private readonly IMessengerService _messengerService;

        public SendMessageManager(ILogger<SendMessageManager> logger, IMessengerService messengerService)
        {
            _logger = logger;
            _messengerService = messengerService;
        }

        public async Task SendMessagesToQueue(ProcessManagerType processManagerType, string eventMessage, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Entering SendMessagesToQueue");

            switch (processManagerType)
            {
                case ProcessManagerType.CKD:
                    {
                        if (eventMessage.EqualsIgnoreCase("EvalReceived"))
                        {
                            await _messengerService.SendMessages<Signify.CKD.Messages.Events.EvalReceived>(processManagerType, eventMessage, stoppingToken);
                            break;
                        }

                        if (eventMessage.EqualsIgnoreCase("PdfDeliveredToClient"))
                        {
                            await _messengerService.SendMessages<Signify.CKD.Svc.Core.Events.PdfDeliveredToClient>(processManagerType, eventMessage, stoppingToken);
                            break;
                        }

                        _logger.LogInformation("Unable to match event message in CKD.  No action will be performed");
                        break;
                    }
                case ProcessManagerType.DEE:
                    {
                        if (eventMessage.EqualsIgnoreCase("CreateDee"))
                        {
                            await _messengerService.SendMessages<Signify.DEE.Svc.Core.Commands.CreateDee>(processManagerType, eventMessage, stoppingToken);
                            break;
                        }

                        if (eventMessage.EqualsIgnoreCase("ProcessDee"))
                        {
                            await _messengerService.SendMessages<Signify.DEE.Svc.Core.Commands.ProcessDee>(processManagerType, eventMessage, stoppingToken);
                            break;
                        }

                        if (eventMessage.EqualsIgnoreCase("ProcessPdfDelivered"))
                        {
                            await _messengerService.SendMessages<Signify.DEE.Svc.Core.Commands.ProcessPdfDelivered>(processManagerType, eventMessage, stoppingToken);
                            break;
                        }

                        if (eventMessage.EqualsIgnoreCase("ProcessIrisResultPdf"))
                        {
                            await _messengerService.SendMessages<Signify.DEE.Svc.Core.Commands.ProcessIrisResultPdf>(processManagerType, eventMessage, stoppingToken);
                            break;
                        }

                        _logger.LogInformation("Unable to match event message in DEE.  No action will be performed");
                        break;
                    }
                case ProcessManagerType.EGFR:
                    {
                        if (eventMessage.EqualsIgnoreCase("EvalReceived"))
                        {
                            await _messengerService.SendMessages<eGFRNsbEvents.EvalReceived>(processManagerType, eventMessage, stoppingToken);
                            break;
                        }

                        _logger.LogInformation("EGFR doesn't have any templates defined yet.  No action will be performed");
                        break;
                    }
                case ProcessManagerType.FOBT:
                    {
                        if (eventMessage.EqualsIgnoreCase("CreateOrderEvent"))
                        {
                            await _messengerService.SendMessages<Signify.FOBT.Messages.Events.CreateOrderEvent>(processManagerType, eventMessage, stoppingToken);
                        }

                        if (eventMessage.EqualsIgnoreCase("PdfDeliveredToClient"))
                        {
                            await _messengerService.SendMessages<Signify.FOBT.Svc.Core.Events.PdfDeliveredToClient>(processManagerType, eventMessage, stoppingToken);
                        }
                        
                        if (eventMessage.EqualsIgnoreCase("HomeAccessResultsReceived"))
                        {
                            await _messengerService.SendMessages<FobtNsbEvents.HomeAccessResultsReceived>(processManagerType, eventMessage, stoppingToken);
                            break;
                        }

                        _logger.LogInformation("Unable to match event message in FOBT.  No action will be performed");
                        break;
                    }
                case ProcessManagerType.HBA1CPOC:
                    {
                        if (eventMessage.EqualsIgnoreCase("CreateHbA1CPoc"))
                        {
                            await _messengerService.SendMessages<Hba1cpoc.Events.CreateHbA1CPoc>(processManagerType, eventMessage, stoppingToken);
                        }

                        if (eventMessage.EqualsIgnoreCase("PdfDeliveredToClient"))
                        {
                            await _messengerService.SendMessages<Signify.HBA1CPOC.Messages.Events.PdfDeliveredToClient>(processManagerType, eventMessage, stoppingToken);
                        }

                        _logger.LogInformation("HBA1CPOC doesn't have any templates defined yet.  No action will be performed");
                        break;
                    }
                case ProcessManagerType.HBA1C:
                    {
                        _logger.LogInformation("HBA1C doesn't have any templates defined yet.  No action will be performed");
                        break;
                    }
                case ProcessManagerType.PAD:
                    {
                        if (eventMessage.EqualsIgnoreCase("CreatePad"))
                        {
                            await _messengerService.SendMessages<Signify.PAD.Svc.Core.Commands.CreatePad>(processManagerType, eventMessage, stoppingToken);
                        }

                        if (eventMessage.EqualsIgnoreCase("PdfDeliveredToClient"))
                        {
                            await _messengerService.SendMessages<Signify.PAD.Svc.Core.Events.PdfDeliveredToClient>(processManagerType, eventMessage, stoppingToken);
                        }

                        _logger.LogInformation("PAD doesn't have any templates defined yet.  No action will be performed");
                        break;
                    }
                case ProcessManagerType.Spirometry:
                    {
                        if (eventMessage.EqualsIgnoreCase("EvalReceived"))
                        {
                            await _messengerService.SendMessages<SpiroNsbEvents.EvalReceived>(processManagerType, eventMessage, stoppingToken);
                        }

                        if (eventMessage.EqualsIgnoreCase("PdfDeliveredToClient"))
                        {
                            await _messengerService.SendMessages<Signify.PAD.Svc.Core.Events.PdfDeliveredToClient>(processManagerType, eventMessage, stoppingToken);
                        }

                        _logger.LogInformation("Spirometry doesn't have any templates defined yet.  No action will be performed");
                        break;
                    }
                default:
                    {
                        _logger.LogInformation("Unable to match to a process manager based on the input.  No action will be performed");
                        break;
                    }
            }

            _logger.LogInformation("Exiting SendMessagesToQueue");
        }
    }
}
