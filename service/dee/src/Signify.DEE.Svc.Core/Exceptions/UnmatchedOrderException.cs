using System;

namespace Signify.DEE.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if an iris order result is received but no matching exam is found in our DB.
/// </summary>
[Serializable]
public class UnmatchedOrderException(string localId, int irisPatientId)
    : Exception($"Unable to match iris order (Iris Patient ID: {irisPatientId} with local Id {localId}")
{
    public string LocalId { get; } = localId;
    public int IrisPatientId { get; } = irisPatientId;
}