namespace Cll;

public static class CllParser
{
    public static string? GetOption(string[] args, int order)
    {
        int i = 0;
        while (i < args.Length)
        {
            if (!args[i].StartsWith('-') && i == order)
                return args[i];
            i++;
        }

        return null;
    }
}
