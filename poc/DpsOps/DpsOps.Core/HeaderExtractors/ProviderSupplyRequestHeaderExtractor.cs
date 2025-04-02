using DpsOps.Core.Exceptions;
using DpsOps.Core.Models.ColumnDefinitions;

namespace DpsOps.Core.HeaderExtractors;

internal class ProviderSupplyRequestHeaderExtractor : HeaderExtractorBase<ProviderSupplyRequestColumns>
{
    /// <inheritdoc />
    public override string FileType => "Provider Supply Request";

    /// <inheritdoc />
    public override IDictionary<ProviderSupplyRequestColumns, int> ExtractHeaders(string[] headerValues)
    {
        try
        {
            return base.ExtractHeaders(headerValues);
        }
        catch (DuplicateHeaderException<ProviderSupplyRequestColumns> ex)
        {
            Console.WriteLine($"{FileType} has a duplicate header type: {ex.Column}");

            throw;
        }
        catch (MissingHeaderException<ProviderSupplyRequestColumns> ex)
        {
            Console.WriteLine($"{FileType} is missing column {ex.Column}");

            if (ex.Column == ProviderSupplyRequestColumns.AddressLine2)
                Console.WriteLine("Did you specify the incorrect file ordering in the command arguments?");

            throw;
        }
    }

    protected override bool TryGetColumnType(string header, out ProviderSupplyRequestColumns columnType)
    {
        switch (header.ToUpper())
        {
            case "NPI":
            case "NPI1":
                columnType = ProviderSupplyRequestColumns.Npi;
                return true;
            case "SUPPLYMAILADDRESSLINE1":
            case "SUPPLYMAILADDRESS1":
                columnType = ProviderSupplyRequestColumns.AddressLine1;
                return true;
            case "SUPPLYMAILADDRESSLINE2":
            case "SUPPLYMAILADDRESS2":
                columnType = ProviderSupplyRequestColumns.AddressLine2;
                return true;
            case "SUPPLYMAILCITY":
                columnType = ProviderSupplyRequestColumns.City;
                return true;
            case "SUPPLYMAILSTATE":
                columnType = ProviderSupplyRequestColumns.State;
                return true;
            case "SUPPLYZIPCODE":
                columnType = ProviderSupplyRequestColumns.ZipCode;
                return true;
            default: // we don't care about any of the other headers
                columnType = default;
                return false;
        }
    }
}
