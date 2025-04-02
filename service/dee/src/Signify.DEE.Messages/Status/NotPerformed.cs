namespace Signify.DEE.Messages.Status;

/// <summary>
/// Status event signifying that an evaluation with DEE product did not have a DEE exam performed
/// </summary>
public class NotPerformed : BaseStatusMessage
{
    /// <summary>
    /// "Top-level" reason as to why a DEE exam was not performed, ie answer to question "Reason retinal imaging
    /// not performed"
    /// </summary>
    public string ReasonType { get; set; }
    /// <summary>
    /// Answer for the question "Reason member refused retinal imaging" or "Reason unable to perform retinal
    /// imaging", depending on the value of <see cref="ReasonType"/>
    /// </summary>
    public string Reason { get; set; }
    /// <summary>
    /// Comes from the Evaluation API; answer value for the optional free-form Notes question detailing why
    /// the testing was not performed
    /// </summary>
    public string ReasonNotes { get; set; }
}