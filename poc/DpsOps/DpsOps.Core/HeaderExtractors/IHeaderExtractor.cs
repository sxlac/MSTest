namespace DpsOps.Core.HeaderExtractors;

internal interface IHeaderExtractor<THeader>
    where THeader : struct, Enum
{
    /// <summary>
    /// Type of input file this extractor can be run against
    /// </summary>
    string FileType { get; }

    /// <summary>
    /// Creates a lookup of Header Column Type -> Index of Header 
    /// </summary>
    /// <param name="headerValues">Values of </param>
    /// <remarks>
    /// Results will only include headers defined in the <see cref="THeader"/> enumeration
    /// </remarks>
    /// <returns>Key: header column type, Value: index of the header</returns>
    IDictionary<THeader, int> ExtractHeaders(string[] headerValues);
}
