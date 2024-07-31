namespace Cll;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class UsedByAttribute(string propertyName) : Attribute
{
    public string PropertyName { get; } = propertyName;
}
