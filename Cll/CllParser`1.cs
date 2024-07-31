using System.Reflection;
using System.Text;

namespace Cll;

public class CllParser<TOptions> where TOptions : new()
{
    private readonly Type OptionsType = typeof(TOptions);

    private readonly Dictionary<string, PropertyInfo> PropsByName = [];
    private readonly Dictionary<string, PropertyInfo> Short2Prop = [];
    private readonly Dictionary<string, PropertyInfo> Long2Prop = [];
    private readonly Dictionary<PropertyInfo, (string Short, string? Long)> Prop2Arg = [];
    private readonly Dictionary<int, PropertyInfo> Options = [];
    private readonly Dictionary<PropertyInfo, List<PropertyInfo>> Dependencies = [];
    private readonly Dictionary<string, PropertyInfo> Mandatories = [];

    public CllParser()
    {
        var props = OptionsType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p =>
            new
            {
                Property = p,
                Option = p.GetCustomAttribute<OptionAttribute>(),
                Argument = p.GetCustomAttribute<ArgumentAttribute>(),
                DependsOn = p.GetCustomAttributes<UsedByAttribute>(),
                Mandatory = p.GetCustomAttribute<MandatoryAttribute>(),
            })
            .ToArray();

        foreach (var prop in props)
        {
            if (prop.Mandatory != null)
                Mandatories.Add(prop.Property.Name, prop.Property);

            PropsByName.Add(prop.Property.Name, prop.Property);

            if (prop.Argument != null && prop.Option != null)
                throw new CllException($"prop: {prop.Property.Name} has both Argument and Option Attribute");

            if (prop.Argument != null)
            {
                if (string.IsNullOrWhiteSpace(prop.Argument.Short))
                    throw new CllArgumentException($"Invalid argument(empty) for property: {prop.Property.Name}");

                Short2Prop.Add(prop.Argument.Short, prop.Property);
                if (!string.IsNullOrWhiteSpace(prop.Argument.Long))
                    Long2Prop.Add(prop.Argument.Long, prop.Property);

                Prop2Arg.Add(prop.Property, (prop.Argument.Short, prop.Argument.Long));
            }

            if (prop.Option != null)
            {
                if (Options.ContainsKey(prop.Option.Order))
                    throw new CllException($"Trying to add option with duplicated order '{prop.Option.Order}'");

                Options.Add(prop.Option.Order, prop.Property);
            }
        }

        foreach (var prop in props)
        {
            foreach (var dep in prop.DependsOn.Select(s => s.PropertyName))
            {
                List<PropertyInfo>? deps;

                if (!PropsByName.TryGetValue(dep, out var main))
                    throw new CllException($"no property with name: '{dep}'");

                if (!Dependencies.TryGetValue(main, out deps))
                {
                    deps = [];
                    Dependencies.Add(main, deps);
                }

                deps.Add(prop.Property);
            }
        }
    }

    public TOptions Parse(string[] args)
    {
        var options = new TOptions();
        var optionIdx = 0;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith('-'))
            {
                PropertyInfo? propInfo;

                if (arg.StartsWith("--"))
                {
                    if (!Long2Prop.TryGetValue(arg.Substring(2), out propInfo))
                        throw new CllUnknownArgument(arg);
                }
                else
                {
                    if (!Short2Prop.TryGetValue(arg.Substring(1), out propInfo))
                        throw new CllUnknownArgument(arg);
                }

                if (propInfo.PropertyType == typeof(bool))
                    SetProperty(arg, options, propInfo, true);
                else
                {
                    i++;
                    if (i < args.Length)
                        SetProperty(arg, options, propInfo, args[i]);
                    else
                        throw new CllParseException($"No value for '{arg}'");
                }
            }
            else
            {
                if (Options.TryGetValue(optionIdx, out var propInfo))
                {
                    SetProperty(propInfo.Name.ToUpper(), options, propInfo, arg);
                }
                else
                    throw new CllArgumentException($"no option: {optionIdx} for [{arg}]");

                optionIdx++;
            }
        }

        return options;
    }

    private static void SetProperty(string arg, object instance, PropertyInfo propInfo, object value)
    {
        if (propInfo.PropertyType == typeof(int))
        {
            if (!int.TryParse((string)value, out int val))
                throw new CllParseException($"Invalid numeric value for: '{arg}'");

            value = val;
        }
        else if (propInfo.PropertyType.IsEnum)
        {
            if (!Enum.TryParse(propInfo.PropertyType, (string)value, true, out var enumValue))
                throw new CllArgumentException($"${arg} can not be set to '{value}'");

            value = enumValue;
        }

        if (propInfo.SetMethod == null)
            throw new CllException($"can not set(no setter) for '{propInfo.Name}'/'{value}'");

        propInfo.SetMethod.Invoke(instance, new[] { value });
    }

    public string GenerateHelp(string executableName, string description) // todo: add assembly info with version, description etc
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{executableName}: {description} usage");
        sb.Append($"\t{executableName} ");

        List<string> args = [];

        foreach (var (p, (Short, Long)) in Prop2Arg.Select(s => (s.Key, s.Value)))
        {
            var mandatory = Mandatories.ContainsKey(p.Name);
            var propSb = new StringBuilder();

            propSb.Append(mandatory ? '<' : '[');
            propSb.Append($"-{Short}");
            if (!string.IsNullOrWhiteSpace(Long))
                propSb.Append($"/--{Long}");

            // todo: enum values using | e.g. [-c/--color red|greed|blue]
            if (p.PropertyType != typeof(bool))
                propSb.Append($" {p.Name.ToLower()}");

            propSb.Append(mandatory ? '>' : ']');
            args.Add(propSb.ToString());
        }

        sb.Append(string.Join(' ', args));
        args.Clear();

        var optionNames = Options
            .Select(s => (s.Key, s.Value))
            .OrderBy(s => s.Key)
            .Select(s => s.Value.Name);

        foreach (var name in optionNames)
        {
            var mandatory = Mandatories.ContainsKey(name);
            var propSb = new StringBuilder();

            propSb.Append(mandatory ? '<' : '[');
            propSb.Append($"{name.ToUpper()}");
            propSb.Append(mandatory ? '>' : ']');
            args.Add(propSb.ToString());
        }
        if (args.Count > 0)
            sb.Append(' ');

        sb.AppendLine(string.Join(' ', args));
        sb.AppendLine();
        sb.AppendLine("argument dependencies:");

        foreach (var (prop, deps) in Dependencies
            .Select(s => (s.Key, s.Value))
            .OrderBy(s => Prop2Arg.ContainsKey(s.Key) ? 1 : 2))
        {
            sb
                .Append("\t")
                .Append(GetPropertyPresentation(prop))
                .Append(" uses ")
                .Append(string.Join(", ", deps.Select(GetPropertyPresentation)))
                .AppendLine();
        }

        return sb.ToString();
    }

    private string GetPropertyPresentation(PropertyInfo prop)
    {
        if (Prop2Arg.ContainsKey(prop))
        {
            var val = Prop2Arg[prop];
            var display = $"-{val.Short}";
            if (!string.IsNullOrEmpty(val.Long))
                display += $"/--{val.Long}";

            return display;
        }

        return prop.Name.ToUpper();
    }
}