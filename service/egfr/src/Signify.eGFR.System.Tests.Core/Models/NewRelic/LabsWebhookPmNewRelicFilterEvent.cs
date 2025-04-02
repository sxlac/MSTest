namespace Signify.eGFR.System.Tests.Core.Models.NewRelic;

/// <summary>
/// Uacr NewRelic Filter Event
/// </summary>
public class UacrNewRelicFilterEvent
{
    public long CreatedDateTime { get; set; }

    public string DpsLabsWebhookPmTestName { get; set; }

    public Guid EventId { get; set; }

    public string Vendor { get; set; }

    public long AppId { get; set; }

    public long Timestamp { get; set; }
}