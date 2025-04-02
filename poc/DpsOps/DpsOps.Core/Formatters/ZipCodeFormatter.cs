using DpsOps.Core.Models.Records;

namespace DpsOps.Core.Formatters;

internal class ZipCodeFormatter : IRecordFormatter<MasterListRecord>,
    IRecordFormatter<SupplyRequestRecord>
{
    /// <inheritdoc />
    public void Format(MasterListRecord record)
    {
        record.ZipCode = Format(record.ZipCode);
    }

    /// <inheritdoc />
    public void Format(SupplyRequestRecord record)
    {
        record.ZipCode = Format(record.ZipCode);
    }

    private static string Format(string zipCode)
    {
        switch (zipCode.Length)
        {
            case 0:
            case 5:
                return zipCode;
        }

        // "1234" -> "01234"
        // "12345-1234" => "12345"
        return zipCode.Length < 5
            ? zipCode.PadLeft(5 - zipCode.Length, '0')
            : zipCode[..5];
    }
}
