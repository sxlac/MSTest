namespace Signify.HBA1CPOC.Messages.Events.Akka;

// ReSharper disable once InconsistentNaming
#pragma warning disable S101 // SonarQube - Types should be named in PascalCase
public class CDIFailedEvent: BaseCdiEvent
#pragma warning restore S101
{
    public string Reason { get; set; }
    public bool PayProvider { get; set; } 
}