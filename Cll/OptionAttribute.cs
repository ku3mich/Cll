namespace Cll;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class OptionAttribute(int order, string? description = null) : Attribute
{
    public int Order { get; } = order;
    public string? Description { get; } = description;
}
