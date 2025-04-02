using CsvHelper;
using System.Text;

namespace Signify.Tools.MessageQueue.Settings
{
    public class CsvSettings
    {
        public Encoding? Encoding { get; set; } = Encoding.UTF8;

        public string Delimiter { get; set; } = string.Empty;

        public bool HasHeaderRecord { get; set; }

        public HeaderValidated? HeaderValidated { get; set; } = null;
    }
}
