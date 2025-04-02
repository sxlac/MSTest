namespace IrisPoc.Models.Image;

/// <summary>
/// Image details from this POC's config file
/// </summary>
public class ImageModel
{
    /// <summary>
    /// For this POC, I'm using images saved on my local filesystem 
    /// </summary>
    public string? FilePath { get; set; }

    /// <remarks>
    /// Supports <c>"left"</c> and <c>"right"</c>, case insensitive, or <c>null</c> to tell Iris the side is unknown
    /// </remarks>
    public string? Side { get; set; }
}
