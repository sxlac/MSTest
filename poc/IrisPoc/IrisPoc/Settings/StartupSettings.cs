using IrisPoc.Models;

namespace IrisPoc.Settings;

public class StartupSettings
{
    /// <summary>
    /// Whether or not to subscribe to results on startup
    /// </summary>
    public bool SubscribeToResults { get; set; }

    /// <summary>
    /// Whether or not to submit orders on startup
    /// </summary>
    public bool SubmitOrders { get; set; }

    public ImageSubmissionMode ImageSubmissionMode { get; set; }

    /// <summary>
    /// Orders to place on startup
    /// </summary>
    public ICollection<ExamModel> Orders { get; set; } = new List<ExamModel>();
}

public enum ImageSubmissionMode
{
    OnOrderCreation,
    AfterOrderCreation
}
