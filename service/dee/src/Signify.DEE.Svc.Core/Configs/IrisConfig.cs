namespace Signify.DEE.Svc.Core.Configs;

public class IrisConfig
{
    public string SiteLocalId { get; set; }
    public string ClientGuid { get; set; }
    public string OrderSubmissionServiceBusConnectionString { get; set; }
    public string ImageUploadConnectionString { get; set; }
    public string OrderEventsServiceBusConnectionString { get; set; }
}