namespace Signify.uACR.System.Tests.Core.Models.NewRelic;

public class FhirParseExceptionEvent
{
    public long CreatedDateTime { get; set; }

    public ErrorType Error { get; set; }

    public long AppId { get; set; }

    public long Timestamp { get; set; }
}

public class ErrorType
{
    public string Class { get; set; }
    public string Message { get; set; }
}