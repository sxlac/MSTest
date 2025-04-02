using DpsOps.Core.Exceptions;
using DpsOps.Core.Models.ColumnDefinitions;

namespace DpsOps.Core.HeaderExtractors;

internal class MasterListHeaderExtractor : HeaderExtractorBase<MasterListColumns>
{
    /// <inheritdoc />
    public override string FileType => "Master List";

    /// <inheritdoc />
    public override IDictionary<MasterListColumns, int> ExtractHeaders(string[] headerValues)
    {
        try
        {
            return base.ExtractHeaders(headerValues);
        }
        catch (DuplicateHeaderException<MasterListColumns> ex)
        {
            Console.WriteLine($"{FileType} has a duplicate header type: {ex.Column}");

            throw;
        }
        catch (MissingHeaderException<MasterListColumns> ex)
        {
            Console.WriteLine($"{FileType} is missing column {ex.Column}");

            throw;
        }
    }

    protected override bool TryGetColumnType(string header, out MasterListColumns columnType)
    {
        switch (header.ToUpper())
        {
            case "NPI":
            case "NPI1":
                columnType = MasterListColumns.Npi;
                return true;
            case "SHIP TO #":
            case "SHIP TO NUMBER":
                columnType = MasterListColumns.ShipToNumber;
                return true;
            case "ADDRESS LINE 1":
                columnType = MasterListColumns.AddressLine1;
                return true;
            case "CITY":
                columnType = MasterListColumns.City;
                return true;
            case "STATE":
                columnType = MasterListColumns.State;
                return true;
            case "ZIP CODE":
                columnType = MasterListColumns.ZipCode;
                return true;
            default: // we don't care about any of the other headers
                columnType = default;
                return false;
        }
    }
}
