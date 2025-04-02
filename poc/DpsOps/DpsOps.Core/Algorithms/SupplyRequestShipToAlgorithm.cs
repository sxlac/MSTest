using DpsOps.Core.Models.Records;
using DpsOps.Core.Models.Results;

namespace DpsOps.Core.Algorithms;

internal interface ISupplyRequestShipToAlgorithm
{
    /// <summary>
    /// Attempts to match the given Supply Request record to a Ship To number
    /// in the given master list
    /// </summary>
    public ShipToMatchResult TryMatch(SupplyRequestRecord record, IEnumerable<MasterListRecord> npiMasterList);
}

internal class SupplyRequestShipToAlgorithm : ISupplyRequestShipToAlgorithm
{
    /// <inheritdoc />
    public ShipToMatchResult TryMatch(SupplyRequestRecord record, IEnumerable<MasterListRecord> npiMasterList)
    {
        if (!HasValidAddress(record))
        {
            return new ShipToMatchResult
            {
                Reason = "Invalid address"
            };
        }

        // First filter out by City, State and Zip
        var potentialMatches = GetPotentialMatchesByCity(record, npiMasterList);
        if (potentialMatches.Count < 1)
        {
            return new ShipToMatchResult
            {
                Reason = "No Potential Matches by City State Zip"
            };
        }

        // Then try matching against full address
        var hasAddressLine2 = !string.IsNullOrEmpty(record.AddressLine2);

        var fullAddress = !hasAddressLine2
            ? record.AddressLine1 : string.Join(' ', record.AddressLine1, record.AddressLine2);

        if (TryMatchFullAddress(fullAddress, potentialMatches, out var result))
            return result;

        // If there are no matches by full address, see if maybe it is just due to Address Line 2 mismatch
        if (hasAddressLine2 && potentialMatches.Exists(each => each.AddressLine1.StartsWith(record.AddressLine1)))
        {
            return new ShipToMatchResult
            {
                Reason = "Address line 2 mismatch"
            };
        }

        return new ShipToMatchResult
        {
            Reason = "No Match by Address"
        };
    }

    private static bool HasValidAddress(SupplyRequestRecord record)
    {
        return !string.IsNullOrEmpty(record.AddressLine1) &&
               !string.IsNullOrEmpty(record.City) &&
               !string.IsNullOrEmpty(record.State) &&
               !string.IsNullOrEmpty(record.ZipCode);
    }

    private static List<MasterListRecord> GetPotentialMatchesByCity(SupplyRequestRecord record,
        IEnumerable<MasterListRecord> npiMasterList)
    {
        return npiMasterList.Where(each =>
                record.ZipCode == each.ZipCode &&
                record.State == each.State &&
                record.City == each.City)
            .ToList();
    }

    private static bool TryMatchFullAddress(string fullAddress, List<MasterListRecord> potentialMatches, out ShipToMatchResult result)
    {
        // Check if there is an exact match
        var match = potentialMatches.Find(each =>
            fullAddress == each.AddressLine1);

        if (match != null)
        {
            result = new ShipToMatchResult
            {
                ShipToNumber = match.ShipToNumber,
                Reason = string.IsNullOrEmpty(match.ShipToNumber) ? "Match has invalid ship to #" : string.Empty
            };

            return true;
        }

        // If no exact match, see if there is a match that is close enough.
        // This is a valid case to consider, especially since it looks at
        // the start of the address first, which starts with street # followed
        // by street name
        var bestMatchLength = 0;
        MasterListRecord bestMatch = null;
        foreach (var potential in potentialMatches)
        {
            var potentialAddress = potential.AddressLine1;

            var min = Math.Min(potentialAddress.Length, fullAddress.Length);

            var lengthMatched = 0;

            for (var i = 0; i < min; ++i)
            {
                if (potentialAddress[i] == fullAddress[i])
                    ++lengthMatched;
                else
                    break;
            }

            if (lengthMatched <= bestMatchLength)
                continue;

            bestMatchLength = lengthMatched;
            bestMatch = potential;
        }

#pragma warning disable S2583 // SonarQube - Change this condition so that it does not always evaluate to 'False' - You're wrong SonarQube...
        if (bestMatchLength > 8) // arbitrary length that seems reasonable
#pragma warning restore S2583
        {
            result = new ShipToMatchResult
            {
                Reason = "Potential Match - Please Review",
                ShipToNumber = bestMatch!.ShipToNumber
            };

            return true;
        }

        result = default;
        return false;
    }
}