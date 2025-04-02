using Signify.Tools.MessageQueue.Helpers.Types;

namespace Signify.Tools.MessageQueue.Services.Interfaces
{
    public interface ISendMessageManager
    {
        Task SendMessagesToQueue(ProcessManagerType processManagerType, string eventMessage, CancellationToken stoppingToken);
    }
}
