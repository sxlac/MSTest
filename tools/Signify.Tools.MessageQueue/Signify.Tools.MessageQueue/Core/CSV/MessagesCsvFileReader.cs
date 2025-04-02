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
    public class MessagesCsvFileReader : IMessagesCsvFileReader
    {
        private readonly ILogger<MessagesCsvFileReader> _logger;
        private readonly CsvSettings _csvSettings;

        public MessagesCsvFileReader(ILogger<MessagesCsvFileReader> logger, IOptions<CsvSettings> csvOptions)
        {
            _logger = logger;
            _csvSettings = csvOptions.Value;
        }

        public async Task<List<T>> ReadMessageValues<T>(FileInfo fileInfo, CancellationToken cancellationToken)
        {
            var messageValues = new List<T>();

            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Encoding = _csvSettings.Encoding,
                Delimiter = _csvSettings.Delimiter,
                HasHeaderRecord = _csvSettings.HasHeaderRecord,
                HeaderValidated = _csvSettings.HeaderValidated
            };

            await using (var fileStream = File.Open(fileInfo!.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var textReader = new StreamReader(fileStream, Encoding.UTF8))
            using (var csv = new CsvReader(textReader, configuration))
            {
                var csvFileRecords = csv.GetRecords<T>();

                foreach (var record in csvFileRecords)
                {
                    messageValues.Add(record);
                }
            }

            return messageValues.ToList();
        }
    }
}
