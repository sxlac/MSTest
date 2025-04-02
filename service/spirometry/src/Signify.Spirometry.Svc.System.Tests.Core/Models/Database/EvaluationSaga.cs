using Newtonsoft.Json;

namespace Signify.Spirometry.Svc.System.Tests.Core.Models.Database;

public class EvaluationSaga
{
    public string Data { get; set; }

    public ClinicalSupport ClinicalSupportData =>
        string.IsNullOrEmpty(Data) ? null : JsonConvert.DeserializeObject<ClinicalSupport>(Data);
}

public class ClinicalSupport
{
    public bool IsBillable { get; set; }  // Property for ClinicalSupport Results
    public bool NeedsFlag { get; set; }  // Property for ClinicalSupport Results
    public int EvaluationId { get; set; }  // Property for ClinicalSupport Results
}