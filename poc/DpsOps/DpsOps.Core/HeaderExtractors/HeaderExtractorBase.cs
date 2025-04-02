using DpsOps.Core.Exceptions;

namespace DpsOps.Core.HeaderExtractors;

internal abstract class HeaderExtractorBase<TColumns> : IHeaderExtractor<TColumns>
    where TColumns : struct, Enum
{
    /// <inheritdoc />
    public abstract string FileType { get; }

    /// <summary>
    /// Given the header, attempt to determine which column type it correlates
    /// to, if defined in the <see cref="TColumns"/> enumeration
    /// </summary>
    /// <param name="header">Header value</param>
    /// <param name="columnType"></param>
    /// <returns>False if the column is not defined in <see cref="TColumns"/></returns>
    protected abstract bool TryGetColumnType(string header, out TColumns columnType);

    /// <inheritdoc />
    public virtual IDictionary<TColumns, int> ExtractHeaders(string[] headerValues)
    {
        var dict = new Dictionary<TColumns, int>();
        for (var i = 0; i < headerValues.Length; ++i)
        {
            if (!TryGetColumnType(headerValues[i], out var columnType))
                continue;

            if (dict.TryAdd(columnType, i))
                continue;

            throw new DuplicateHeaderException<TColumns>(columnType);
        }

        foreach (var columnType in Enum.GetValues<TColumns>())
        {
            if (dict.TryGetValue(columnType, out _))
                continue;

            throw new MissingHeaderException<TColumns>(columnType);
        }

        return dict;
    }
}
