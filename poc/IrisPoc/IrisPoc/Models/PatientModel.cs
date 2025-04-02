namespace IrisPoc.Models;

/// <summary>
/// Patient model taken pretty much copy-paste from DEE, allowing us to map DEE models to IRIS models in this POC
/// </summary>
public class PatientModel
{
    /// <summary>
    /// Corresponds to MemberPlanId in DEE
    /// </summary>
    public long PatientId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Gender { get; set; }
    public DateTime BirthDate { get; set; }
    /// <summary>
    /// 2-character short code for their US State
    /// </summary>
    public string? State { get; set; }
}