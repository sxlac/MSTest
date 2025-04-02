using Microsoft.Extensions.Logging;
using Signify.Tools.MessageQueue.Core.Interfaces;
using Signify.Tools.MessageQueue.Helpers.Extensions;
using Signify.Tools.MessageQueue.Helpers.Types;
using Signify.Tools.MessageQueue.Services.Interfaces;

namespace Signify.Tools.MessageQueue.Services
{
    public class TemplateFileService : ITemplateFileService
    {
        private readonly ILogger<TemplateFileService> _logger;
        private readonly ITemplateCsvFileWriter _templateCsvFileWriter;

        public TemplateFileService(ILogger<TemplateFileService> logger, ITemplateCsvFileWriter templateCsvFileWriter)
        {
            _logger = logger;
            _templateCsvFileWriter = templateCsvFileWriter;
        }

        public async Task Generate(ProcessManagerType processManagerType, string eventMessage, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Began Template File Generation");

            switch (processManagerType)
            {
                case ProcessManagerType.CKD:
                    {
                        if (eventMessage.EqualsIgnoreCase("EvalReceived"))
                        {
                            await _templateCsvFileWriter.WriteFile(new Signify.CKD.Messages.Events.EvalReceived(), cancellationToken);
                            break;
                        }

                        if (eventMessage.EqualsIgnoreCase("PdfDeliveredToClient"))
                        {
                            await _templateCsvFileWriter.WriteFile(new Signify.CKD.Svc.Core.Events.PdfDeliveredToClient(), cancellationToken);
                            break;
                        }

                        _logger.LogInformation("CKD doesn't have any templates defined yet.  No action will be performed");
                        break;
                    }
                case ProcessManagerType.DEE:
                    {
                        if (eventMessage.EqualsIgnoreCase("CreateDee"))
                        {
                            await _templateCsvFileWriter.WriteFile(new Signify.DEE.Svc.Core.Commands.CreateDee(), cancellationToken);
                            break;
                        }

                        if (eventMessage.EqualsIgnoreCase("ProcessDee"))
                        {
                            await _templateCsvFileWriter.WriteFile(new Signify.DEE.Svc.Core.Commands.ProcessDee(), cancellationToken);
                            break;
                        }

                        if (eventMessage.EqualsIgnoreCase("ProcessPdfDelivered"))
                        {
                            await _templateCsvFileWriter.WriteFile(new Signify.DEE.Svc.Core.Commands.ProcessPdfDelivered(), cancellationToken);
                            break;
                        }

                        if (eventMessage.EqualsIgnoreCase("ProcessIrisResultPdf"))
                        {
                            await _templateCsvFileWriter.WriteFile(new Signify.DEE.Svc.Core.Commands.ProcessIrisResultPdf(), cancellationToken);
                            break;
                        }

                        _logger.LogInformation("Unable to match event message in DEE.  No action will be performed");
                        break;
                    }
                case ProcessManagerType.EGFR:
                    {
                        if (eventMessage.EqualsIgnoreCase("EvalReceived"))
                        {
                            await _templateCsvFileWriter.WriteFile(new eGFRNsbEvents.EvalReceived(), cancellationToken);
                            break;
                        }

                        _logger.LogInformation("EGFR doesn't have any templates defined yet.  No action will be performed");
                        break;
                    }
                case ProcessManagerType.FOBT:
                    {
                        if (eventMessage.EqualsIgnoreCase("PdfDeliveredToClient"))
                        {
                            await _templateCsvFileWriter.WriteFile(new Signify.FOBT.Svc.Core.Events.PdfDeliveredToClient(), cancellationToken);
                            break;
                        }

                        if (eventMessage.EqualsIgnoreCase("CreateOrderEvent"))
                        {
                            await _templateCsvFileWriter.WriteFile(new Signify.FOBT.Messages.Events.CreateOrderEvent(), cancellationToken);
                            break;
                        }
                        
                        if (eventMessage.EqualsIgnoreCase("HomeAccessResultsReceived"))
                        {
                            await _templateCsvFileWriter.WriteFile(new FobtNsbEvents.HomeAccessResultsReceived(), cancellationToken);
                            break;
                        }

                        _logger.LogInformation("Unable to match event message in FOBT.  No action will be performed");
                        break;
                    }
                case ProcessManagerType.HBA1CPOC:
                    {
                        if (eventMessage.EqualsIgnoreCase("CreateHbA1CPoc"))
                        {
                            await _templateCsvFileWriter.WriteFile(new Hba1cpoc.Events.CreateHbA1CPoc(), cancellationToken);
                            break;
                        }

                        if (eventMessage.EqualsIgnoreCase("PdfDeliveredToClient"))
                        {
                            await _templateCsvFileWriter.WriteFile(new Signify.HBA1CPOC.Messages.Events.PdfDeliveredToClient(), cancellationToken);
                            break;
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
                            await _templateCsvFileWriter.WriteFile(new Signify.PAD.Svc.Core.Commands.CreatePad(), cancellationToken);
                            break;
                        }

                        if (eventMessage.EqualsIgnoreCase("PdfDeliveredToClient"))
                        {
                            await _templateCsvFileWriter.WriteFile(new Signify.PAD.Svc.Core.Events.PdfDeliveredToClient(), cancellationToken);
                            break;
                        }

                        _logger.LogInformation("PAD doesn't have any templates defined yet.  No action will be performed");
                        break;
                    }
                case ProcessManagerType.Spirometry:
                    {
                        if (eventMessage.EqualsIgnoreCase("EvalReceived"))
                        {
                            await _templateCsvFileWriter.WriteFile(new SpiroNsbEvents.EvalReceived(), cancellationToken);
                            break;
                        }

                        if (eventMessage.EqualsIgnoreCase("PdfDeliveredToClient"))
                        {
                            await _templateCsvFileWriter.WriteFile(new SpiroEvents.PdfDeliveredToClient(), cancellationToken);
                            break;
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

            _logger.LogInformation("End Template File Generation");
        }
    }
}
