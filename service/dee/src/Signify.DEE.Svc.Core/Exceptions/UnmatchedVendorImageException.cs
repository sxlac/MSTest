using System;

namespace Signify.DEE.Svc.Core.Exceptions;

public class UnmatchedVendorImageException(string vendorImageLocalId, int examLocalId)
    : Exception($"Received an image with VendorImageLocalId {vendorImageLocalId} that does not belong to our exam {examLocalId}")
{
    public string VendorImageLocalId { get; } = vendorImageLocalId;
    public int ExamLocalId { get; } = examLocalId;
}