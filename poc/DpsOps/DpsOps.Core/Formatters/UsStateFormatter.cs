using DpsOps.Core.Models.Records;

namespace DpsOps.Core.Formatters;

internal class UsStateFormatter : IRecordFormatter<MasterListRecord>,
    IRecordFormatter<SupplyRequestRecord>
{
    /// <inheritdoc />
    public void Format(MasterListRecord record)
    {
        record.State = Format(record.State);
    }

    /// <inheritdoc />
    public void Format(SupplyRequestRecord record)
    {
        record.State = Format(record.State);
    }

    private static string Format(string state)
    {
        switch (state.Length)
        {
            case 0:
            case 1:
            case 2:
                return state;
        }

        switch (state)
        {
            case "ALABAMA":
            case "ALA":
                return "AL";
            case "ALASKA":
                return "AK";
            case "ARIZONA":
            case "ARIZ":
                return "AZ";
            case "ARKANSAS":
            case "ARK":
                return "AR";
            case "CALIFORNIA":
            case "CAL":
            case "CALIF":
                return "CA";
            case "COLORADO":
            case "COLO":
                return "CO";
            case "CONNECTICUT":
            case "CONNETICUT":
            case "CONN":
                return "CT";
            case "DELAWARE":
            case "DEL":
                return "DE";
            case "DISTRICT OF COLUMBIA":
            case "D C":
                return "DC";
            case "FLORIDA":
            case "FLA":
                return "FL";
            case "GEORGIA":
                return "GA";
            case "HAWAII":
                return "HI";
            case "IDAHO":
            case "IDA":
                return "ID";
            case "ILLINOIS":
            case "ILL":
                return "IL";
            case "INDIANA":
            case "INDIANNA":
            case "IND":
                return "IN";
            case "IOWA":
            case "IOA":
                return "IA";
            case "KANSAS":
            case "KANS":
            case "KAN":
                return "KS";
            case "KENTUCKY":
            case "KENT":
            case "KEN":
                return "KY";
            case "LOUISIANA":
                return "LA";
            case "MAINE":
                return "ME";
            case "MARYLAND":
            case "MARY":
            case "MAR":
                return "MD";
            case "MASSACHUSETTS":
            case "MASS":
                return "MA";
            case "MICHIGAN":
            case "MICH":
                return "MI";
            case "MINNESOTA":
            case "MINN":
                return "MN";
            case "MISSISSIPPI":
            case "MISS":
                return "MS";
            case "MISSOURI":
                return "MO";
            case "MONTANA":
            case "MONT":
                return "MT";
            case "NEBRASKA":
            case "NEBR":
            case "NEB":
                return "NE";
            case "NEVADA":
            case "NEV":
                return "NV";
            case "NEW HAMPSHIRE":
            case "N HAMPSHIRE":
                return "NH";
            case "NEW JERSEY":
            case "N JERSEY":
                return "NJ";
            case "NEW MEXICO":
            case "N MEXICO":
                return "NM";
            case "NEW YORK":
            case "N YORK":
                return "NY";
            case "NORTH CAROLINA":
            case "N CAROLINA":
                return "NC";
            case "NORTH DAKOTA":
            case "N DAKOTA":
                return "ND";
            case "OHIO":
                return "OH";
            case "OKLAHOMA":
            case "OKLA":
                return "OK";
            case "OREGON":
            case "ORE":
                return "OR";
            case "PENNSYLVANIA":
            case "PENN":
                return "PA";
            case "RHODE ISLAND":
            case "R ISLAND":
                return "RI";
            case "SOUTH CAROLINA":
            case "S CAROLINA":
                return "SC";
            case "SOUTH DAKOTA":
            case "S DAKOTA":
                return "SD";
            case "TENNESSEE":
            case "TENN":
                return "TN";
            case "TEXAS":
            case "TEX":
                return "TX";
            case "UTAH":
                return "UT";
            case "VERMONT":
                return "VT";
            case "VIRGINIA":
                return "VA";
            case "WASHINGTON":
                return "WA";
            case "WEST VIRGINIA":
            case "W VIRGINIA":
                return "WV";
            case "WISCONSIN":
            case "WISC":
                return "WI";
            case "WYOMING":
            case "WYO":
                return "WY";
            case "AMERICAN SAMOA":
                return "AS";
            case "GUAM":
                return "GU";
            case "NORTHERN MARIANA ISLANDS":
            case "N MARIANA ISLANDS":
                return "MP";
            case "PUERTO RICO":
                return "PR";
            case "UNITED STATES VIRGIN ISLANDS":
            case "US VIRGIN ISLANDS":
                return "VI";
        }

        return state;
    }
}
