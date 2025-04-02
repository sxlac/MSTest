namespace Signify.Dps.LabResultApi.CouchbasePoc.Configs;

public interface IDocumentControllerConfig
{
    public string InputDocumentFilePath { get; }
}

public class DocumentControllerConfig : IDocumentControllerConfig
{
    public const string ConfigName = "DocumentController";

    public string InputDocumentFilePath { get; set; }
}
