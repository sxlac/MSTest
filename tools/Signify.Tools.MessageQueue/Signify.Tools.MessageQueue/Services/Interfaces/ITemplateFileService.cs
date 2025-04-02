using Signify.Tools.MessageQueue.Helpers.Types;

namespace Signify.Tools.MessageQueue.Services.Interfaces
{
    public interface ITemplateFileService
    {
        Task Generate(ProcessManagerType processManagerType, string eventMessage, CancellationToken cancellationToken);
    }
}
