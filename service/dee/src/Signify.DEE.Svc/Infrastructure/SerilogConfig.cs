namespace Signify.DEE.Svc.Infrastructure;

public class SerilogConfig
{
    public Properties Properties { get; set; }
}

public class Properties
{

    public string Environment { get; set; }
    public string App { get; set; }
    public string Version { get; set; }
}