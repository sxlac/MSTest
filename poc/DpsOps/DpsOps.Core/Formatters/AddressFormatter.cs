using DpsOps.Core.Models.Records;

namespace DpsOps.Core.Formatters;

internal class AddressFormatter : IRecordFormatter<MasterListRecord>,
    IRecordFormatter<SupplyRequestRecord>
{
    private const char Space = ' ';

    /// <inheritdoc />
    public void Format(MasterListRecord record)
    {
        record.AddressLine1 = Format(record.AddressLine1);
    }

    /// <inheritdoc />
    public void Format(SupplyRequestRecord record)
    {
        record.AddressLine1 = Format(record.AddressLine1);
        record.AddressLine2 = Format(record.AddressLine2);
    }

    private static string Format(string address)
    {
        var parts = address.Split(Space);

        for (var i = 0; i < parts.Length; ++i)
        {
            // List taken from USPS website
            switch (parts[i])
            {
                case "AVE":
                    parts[i] = "AVENUE";
                    break;
                case "BLDG":
                    parts[i] = "BUILDING";
                    break;
                case "BLVD":
                    parts[i] = "BOULEVARD";
                    break;
                case "CIR":
                    parts[i] = "CIRCLE";
                    break;
                case "CRK":
                    parts[i] = "CREEK";
                    break;
                case "CT":
                    parts[i] = "COURT";
                    break;
                case "CTS":
                    parts[i] = "COURTS";
                    break;
                case "CTR":
                    parts[i] = "CENTER";
                    break;
                case "CV":
                    parts[i] = "COVE";
                    break;
                case "DR":
                case "DRV":
                    parts[i] = "DRIVE";
                    break;
                case "EXP":
                case "EXPR":
                case "EXPRESS":
                case "EXPW":
                case "EXPY":
                    parts[i] = "EXPRESSWAY";
                    break;
                case "FT":
                    parts[i] = "FORT";
                    break;
                case "FWY":
                case "FRWY":
                case "FRWAY":
                case "FREEWY":
                    parts[i] = "FREEWAY";
                    break;
                case "GRN":
                    parts[i] = "GREEN";
                    break;
                case "GRV":
                    parts[i] = "GROVE";
                    break;
                case "HL":
                    parts[i] = "HILL";
                    break;
                case "HLS":
                    parts[i] = "HILLS";
                    break;
                case "HT":
                case "HTS":
                    parts[i] = "HEIGHTS";
                    break;
                case "HW":
                case "HWY":
                case "HWAY":
                case "HIWY":
                case "HIWAY":
                case "HIGHWY":
                    parts[i] = "HIGHWAY";
                    break;
                case "JCT":
                case "JCTION":
                case "JCTN":
                case "JUNCTN":
                case "JUNCTON":
                    parts[i] = "JUNCTION";
                    break;
                case "LK":
                    parts[i] = "LAKE";
                    break;
                case "LN":
                    parts[i] = "LANE";
                    break;
                case "MT":
                case "MNT":
                    parts[i] = "MOUNT";
                    break;
                case "MTN":
                    parts[i] = "MOUNTAIN";
                    break;
                case "ORCH":
                    parts[i] = "ORCHARD";
                    break;
                case "OVL":
                    parts[i] = "OVAL";
                    break;
                case "PL":
                    parts[i] = "PLACE";
                    break;
                case "PLN":
                    parts[i] = "PLAIN";
                    break;
                case "PLNS":
                    parts[i] = "PLAINS";
                    break;
                case "PLZ":
                case "PLZA":
                    parts[i] = "PLAZA";
                    break;
                case "PRK":
                    parts[i] = "PARK";
                    break;
                case "PKY":
                case "PKWY":
                case "PKWAY":
                case "PARKWY":
                    parts[i] = "PARKWAY";
                    break;
                case "PRKWYS":
                    parts[i] = "PARKWAYS";
                    break;
                case "PR":
                    parts[i] = "PRAIRIE";
                    break;
                case "PT":
                    parts[i] = "POINT";
                    break;
                case "PTS":
                    parts[i] = "POINTS";
                    break;
                case "RD":
                    parts[i] = "ROAD";
                    break;
                case "RDG":
                    parts[i] = "RIDGE";
                    break;
                case "RIV":
                case "RVR":
                    parts[i] = "RIVER";
                    break;
                case "SQ":
                case "SQR":
                    parts[i] = "SQUARE";
                    break;
                case "ST":
                case "STR":
                case "STRT":
                    parts[i] = "STREET";
                    break;
                case "STA":
                case "STN":
                    parts[i] = "STATION";
                    break;
                case "SMT":
                case "SUMIT":
                    parts[i] = "SUMMIT";
                    break;
                case "TER":
                case "TERR":
                    parts[i] = "TERRACE";
                    break;
                case "TRL":
                case "TRLS":
                case "TRAIL":
                    parts[i] = "TRAILS";
                    break;
                case "TRNPK":
                case "TURNPK":
                    parts[i] = "TURNPIKE";
                    break;
                case "VLY":
                case "VLLY":
                case "VALLY":
                    parts[i] = "VALLEY";
                    break;
                case "VW":
                    parts[i] = "VIEW";
                    break;
                case "WY":
                    parts[i] = "WAY";
                    break;

                case "APT":
                    parts[i] = "APARTMENT";
                    break;

                case "N":
                    parts[i] = "NORTH";
                    break;
                case "NE":
                case "NORTHEAST":
                    parts[i] = "NORTH EAST";
                    break;
                case "NW":
                case "NORTHWEST":
                    parts[i] = "NORTH WEST";
                    break;
                case "S":
                    parts[i] = "SOUTH";
                    break;
                case "SE":
                case "SOUTHEAST":
                    parts[i] = "SOUTH EAST";
                    break;
                case "SW":
                case "SOUTHWEST":
                    parts[i] = "SOUTH WEST";
                    break;
                case "E":
                    parts[i] = "EAST";
                    break;
                case "W":
                    parts[i] = "WEST";
                    break;
            }
        }

        return string.Join(Space, parts);
    }
}
