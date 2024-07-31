namespace Cll;

public class CllArgumentException : CllException
{
    public CllArgumentException()
    {
    }

    public CllArgumentException(string? message) : base(message)
    {
    }

    public CllArgumentException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
