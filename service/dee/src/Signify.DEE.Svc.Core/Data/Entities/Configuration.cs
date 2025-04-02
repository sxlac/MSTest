using System;

namespace Signify.DEE.Svc.Core.Data.Entities;

public partial class Configuration
{
    public int ConfigurationId { get; set; }
    public string ConfigurationName { get; set; }
    public string ConfigurationValue { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
}