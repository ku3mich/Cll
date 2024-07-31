namespace Cll;

public class CllException : Exception
{
    public CllException()
    {
    }

    public CllException(string? message) : base(message)
    {
    }

    public CllException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
