namespace DpsOps.Core.Exceptions;

internal class DuplicateHeaderException<TColumns>(TColumns column)
    : DuplicateHeaderException($"Duplicate header exists for column {column}")
    where TColumns : struct, Enum
{
    public TColumns Column { get; } = column;
}

public abstract class DuplicateHeaderException(string message) : Exception(message);
