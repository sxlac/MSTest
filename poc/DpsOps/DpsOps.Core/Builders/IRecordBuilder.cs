using DpsOps.Core.Formatters;

namespace DpsOps.Core.Builders;

/// <summary>
/// Builds a record of type <see cref="T"/> from a string
/// </summary>
/// <typeparam name="T">Type of record to build</typeparam>
internal interface IRecordBuilder<out T>
{
    /// <summary>
    /// Adds a formatter to use when building records
    /// </summary>
    /// <returns>Self</returns>
    IRecordBuilder<T> AddFormatter(IRecordFormatter<T> formatter);

    /// <summary>
    /// Builds a record from the given string
    /// </summary>
    T BuildRecord(string line);
}
