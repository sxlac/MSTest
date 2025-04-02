using System;

namespace Signify.FOBT.Svc.Core.Exceptions;

public class DuplicateBarcodeFoundException : Exception
{
    public string Barcode { get; }
    /// <summary>
    /// Duplicate barcode found by the given <paramref name="barcode"/>
    /// </summary>
    public DuplicateBarcodeFoundException(string barcode)
        : base($"Duplicate Barcode found, for Barcode {barcode}")
    {
        Barcode = barcode;
    }
}