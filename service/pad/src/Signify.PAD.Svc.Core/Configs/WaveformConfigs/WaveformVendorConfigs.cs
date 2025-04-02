using System.Collections.Generic;

namespace Signify.PAD.Svc.Core.Configs.WaveformConfigs;

public interface IWaveformVendorsConfig
{
    IEnumerable<IWaveformVendorConfig> VendorConfigs { get; }
}

public interface IWaveformVendorConfig
{
    /// <summary>
    /// Name of the vendor
    /// </summary>
    string VendorName { get; }
    /// <summary>
    /// Name of the directory where the vendor's waveform files will be placed
    /// </summary>
    string VendorDirectory { get; }
    /// <summary>
    /// Naming format for the vendor's waveform files
    /// </summary>
    string FileNameFormat { get; }
}

public class WaveformVendorConfig : IWaveformVendorConfig
{
    /// <inheritdoc />
    public string VendorName { get; set; }
    /// <inheritdoc />
    public string VendorDirectory { get; set; }
    /// <inheritdoc />
    public string FileNameFormat { get; set; }
}
