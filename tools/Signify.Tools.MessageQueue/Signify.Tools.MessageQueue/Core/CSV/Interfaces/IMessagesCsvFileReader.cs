namespace Signify.Tools.MessageQueue.Core.Interfaces
{
    public interface IMessagesCsvFileReader
    {
        Task<List<T>> ReadMessageValues<T>(FileInfo fileInfo, CancellationToken cancellationToken);
    }
}
