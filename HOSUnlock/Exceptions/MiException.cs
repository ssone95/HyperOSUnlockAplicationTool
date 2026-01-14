namespace HOSUnlock.Exceptions;

/// <summary>
/// Exception thrown when Mi API operations fail.
/// </summary>
public sealed class MiException : Exception
{
    public int StatusCode { get; }

    public MiException() : base()
    {
        StatusCode = -1;
    }

    public MiException(string message, int statusCode = -1)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public MiException(string message, Exception innerException, int statusCode = -1)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
