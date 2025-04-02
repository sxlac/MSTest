using System;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace Signify.DEE.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public partial class SchemaVersion
{
    public int InstalledRank { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public string Script { get; set; }
    public int? Checksum { get; set; }
    public string InstalledBy { get; set; }
    public DateTime InstalledOn { get; set; }
    public int ExecutionTime { get; set; }
    public bool Success { get; set; }
}