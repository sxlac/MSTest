using DpsOps.Core.Formatters;
using DpsOps.Core.Models.ColumnDefinitions;
using DpsOps.Core.Models.Records;

namespace DpsOps.Core.Builders;

internal class SupplyRequestRecordBuilder : RecordBuilderBase<ProviderSupplyRequestColumns>,
    IRecordBuilder<SupplyRequestRecord>
{
    private readonly string _delimiter;

    private readonly List<IRecordFormatter<SupplyRequestRecord>> _formatters = [];

    /// <param name="delimiter"></param>
    /// <param name="columns">Key: column, Value: index of column</param>
    public SupplyRequestRecordBuilder(string delimiter,
        IDictionary<ProviderSupplyRequestColumns, int> columns)
        : base(columns)
    {
        _delimiter = delimiter;
    }

    /// <inheritdoc />
    public IRecordBuilder<SupplyRequestRecord> AddFormatter(IRecordFormatter<SupplyRequestRecord> formatter)
    {
        _formatters.Add(formatter);
        return this;
    }

    /// <inheritdoc />
    public SupplyRequestRecord BuildRecord(string line)
    {
        var parts = line.Split(_delimiter);

        var record = new SupplyRequestRecord
        {
            Npi = Extract(parts, ProviderSupplyRequestColumns.Npi),

            AddressLine1 = Extract(parts, ProviderSupplyRequestColumns.AddressLine1),
            AddressLine2 = Extract(parts, ProviderSupplyRequestColumns.AddressLine2),
            City = Extract(parts, ProviderSupplyRequestColumns.City),
            State = Extract(parts, ProviderSupplyRequestColumns.State),
            ZipCode = Extract(parts, ProviderSupplyRequestColumns.ZipCode)
        };

        foreach (var formatter in _formatters)
        {
            formatter.Format(record);
        }

        return record;
    }
}
