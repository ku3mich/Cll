namespace Cll;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ArgumentAttribute(string @short, string? @long = null, string? description = null) : Attribute
{
    public string Short { get; } = @short;
    public string? Long { get; } = @long;
    public string? Description { get; } = description;
}
