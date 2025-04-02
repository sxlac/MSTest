namespace Signify.Tools.MessageQueue.Settings.ProcessManagers
{
    public class ProcessManagerSettings
    {
        public string QueueName { get; set; } = string.Empty;

        public int ConcurrencyLimit { get; set; }
    }
}
