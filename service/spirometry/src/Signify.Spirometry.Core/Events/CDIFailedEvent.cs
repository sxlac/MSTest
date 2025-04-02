using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Events;

// ReSharper disable once InconsistentNaming
#pragma warning disable S101 // SonarQube - Types should be named in PascalCase
[ExcludeFromCodeCoverage]
public class CDIFailedEvent: CdiEventBase
#pragma warning restore S101
{
    /// <summary>
    /// CDI fail reason
    /// </summary>
    public string Reason { get; set; }
    
    /// <summary>
    /// Indicates whether the Provider should be paid for the IHE
    /// even though CDI failed
    /// </summary>
    public bool PayProvider { get; set; } 
}