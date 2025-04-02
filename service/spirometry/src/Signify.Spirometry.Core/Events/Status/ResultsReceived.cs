namespace Signify.Spirometry.Core.Events.Status
{
    /// <summary>
    /// Status event signifying that spirometry exam results have been received
    /// </summary>
    /// <remarks>
    /// This corresponds to when clinically-valid POC results are received (ie
    /// when the Finalized event occurs), or when overread results are received
    /// (when POC results are clinically-invalid)
    /// </remarks>
    public class ResultsReceived : BaseStatusMessage
    {

    }
}
