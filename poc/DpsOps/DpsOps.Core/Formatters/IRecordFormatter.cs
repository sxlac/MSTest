namespace DpsOps.Core.Formatters;

internal interface IRecordFormatter<in TRecord>
{
    /// <summary>
    /// Formats one or more properties of the given <see cref="record"/>
    /// </summary>
    void Format(TRecord record);
}
