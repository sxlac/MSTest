using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Signify.Tools.MessageQueue.Core.Interfaces;
using Signify.Tools.MessageQueue.Settings;
using System.Globalization;
using System.Text;

namespace Signify.Tools.MessageQueue.Core.CSV
{
    public class TemplateCsvFileWriter : ITemplateCsvFileWriter
    {
        private readonly ILogger<TemplateCsvFileWriter> _logger;
        private readonly CsvSettings _csvSettings;
        private readonly NServiceBusSettings _nServiceBusSettings;

        public TemplateCsvFileWriter
        (
            ILogger<TemplateCsvFileWriter> logger,
            IOptions<CsvSettings> csvOptions,
            IOptions<NServiceBusSettings> serviceBusSettings
        )
        {
            _logger = logger;
            _csvSettings = csvOptions.Value;
            _nServiceBusSettings = serviceBusSettings.Value;
        }

        public async Task WriteFile<T>(T inputType, CancellationToken cancellationToken)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Encoding = _csvSettings.Encoding,
                Delimiter = _csvSettings.Delimiter,
                HasHeaderRecord = _csvSettings.HasHeaderRecord,
                HeaderValidated = _csvSettings.HeaderValidated
            };

            var fileName = $"{_nServiceBusSettings.OutputFileLocation}{inputType!.GetType().Name}_Template_{DateTime.Now.ToString("yyyymmddhhmmss")}.csv";
            await using var textWriter = new StreamWriter(fileName, false, Encoding.UTF8);
            using var csv = new CsvWriter(textWriter, configuration, false);
            csv.WriteHeader<T>();
        }
    }
}
