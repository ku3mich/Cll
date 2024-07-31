namespace Cll;

public class CllUnknownArgument : CllException
{
    public CllUnknownArgument()
    {
    }

    public CllUnknownArgument(string? message) : base(message)
    {
    }

    public CllUnknownArgument(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
