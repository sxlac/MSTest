namespace DpsOps.Core.Exceptions;

internal class MissingHeaderException<TColumns>(TColumns column)
    : MissingHeaderException($"Header for {column} is missing")
    where TColumns : struct, Enum
{
    public TColumns Column { get; } = column;
}

public abstract class MissingHeaderException(string message) : Exception(message);
