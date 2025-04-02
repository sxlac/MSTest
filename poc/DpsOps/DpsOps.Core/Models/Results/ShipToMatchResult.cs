namespace DpsOps.Core.Models.Results;

public class ShipToMatchResult
{
    /// <summary>
    /// Reason for no matching <see cref="ShipToNumber"/>,
    /// or why the value of <see cref="ShipToNumber"/> may need to be manually reviewed 
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    public string ShipToNumber { get; init; } = string.Empty;
}
