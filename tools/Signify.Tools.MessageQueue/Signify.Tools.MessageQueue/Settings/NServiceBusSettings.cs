using Signify.Tools.MessageQueue.Settings.ProcessManagers;

namespace Signify.Tools.MessageQueue.Settings
{
    public class NServiceBusSettings
    {
        public string ProcessManager { get; set; } = string.Empty;

        public string EventMessage { get; set; } = string.Empty;

        public string ActionType { get; set; } = string.Empty;

        public string InputFileLocationAndName { get; set; } = string.Empty;

        public string OutputFileLocation { get; set; } = string.Empty;

        public CkdSettings? CkdSettings { get; set; }

        public DeeSettings? DeeSettings { get; set; }

        public EgfrSettings? EgfrSettings { get; set; }

        public FobtSettings? FobtSettings { get; set; }

        public HbA1cPocSettings? HbA1cPocSettings { get; set; }

        public HbA1CSettings? HbA1CSettings { get; set; }

        public PadSettings? PadSettings { get; set; }

        public SpirometrySettings? SpirometrySettings { get; set; }
    }
}
