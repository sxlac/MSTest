using DpsOps.Core.Formatters;
using DpsOps.Core.Models.ColumnDefinitions;
using DpsOps.Core.Models.Records;

namespace DpsOps.Core.Builders;

internal class MasterListRecordBuilder : RecordBuilderBase<MasterListColumns>,
    IRecordBuilder<MasterListRecord>
{
    private readonly string _delimiter;

    private readonly List<IRecordFormatter<MasterListRecord>> _formatters = [];

    /// <param name="delimiter"></param>
    /// <param name="columns">Key: column, Value: index of column</param>
    public MasterListRecordBuilder(string delimiter,
        IDictionary<MasterListColumns, int> columns)
        : base(columns)
    {
        _delimiter = delimiter;
    }

    /// <inheritdoc />
    public IRecordBuilder<MasterListRecord> AddFormatter(IRecordFormatter<MasterListRecord> formatter)
    {
        _formatters.Add(formatter);
        return this;
    }

    /// <inheritdoc />
    public MasterListRecord BuildRecord(string line)
    {
        var parts = line.Split(_delimiter);

        var record = new MasterListRecord
        {
            Npi = Extract(parts, MasterListColumns.Npi),
            ShipToNumber = Extract(parts, MasterListColumns.ShipToNumber),

            AddressLine1 = Extract(parts, MasterListColumns.AddressLine1),
            City = Extract(parts, MasterListColumns.City),
            State = Extract(parts, MasterListColumns.State),
            ZipCode = Extract(parts, MasterListColumns.ZipCode)
        };

        foreach (var formatter in _formatters)
        {
            formatter.Format(record);
        }

        return record;
    }
}
