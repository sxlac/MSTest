namespace Signify.Tools.MessageQueue.Core.Interfaces
{
    public interface ITemplateCsvFileWriter
    {
        Task WriteFile<T>(T inputType, CancellationToken cancellationToken);
    }
}
