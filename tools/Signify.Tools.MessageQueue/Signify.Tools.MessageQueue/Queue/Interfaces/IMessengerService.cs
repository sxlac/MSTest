using Signify.Tools.MessageQueue.Helpers.Types;

namespace Signify.Tools.MessageQueue.Queue.Interfaces
{
    public interface IMessengerService
    {
        Task SendMessages<T>(ProcessManagerType processManagerType, string eventMessage, CancellationToken cancellation);
    }
}
