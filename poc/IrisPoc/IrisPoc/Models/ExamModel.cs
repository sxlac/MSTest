using IrisPoc.Models.Image;

namespace IrisPoc.Models;

/// <summary>
/// Exam model taken pretty much copy-paste from DEE, allowing us to map DEE models to IRIS models in this POC
/// </summary>
public class ExamModel
{
    public DateTime DateOfService { get; set; }
    /// <summary>
    /// 2-character short code for the US State where the exam was performed
    /// </summary>
    public string? State { get; set; }
    public PatientModel? Patient { get; set; }
    public ProviderModel? Provider { get; set; }

    public IList<ImageModel> Images { get; set; } = new List<ImageModel>();
}
