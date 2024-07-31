namespace Cll;

public class CllParseException : CllException
{
    public CllParseException()
    {
    }

    public CllParseException(string? message) : base(message)
    {
    }

    public CllParseException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
