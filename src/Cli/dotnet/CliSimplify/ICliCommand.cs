// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.DotNet.Cli.CliSimplify;

public interface ICliCommand
{
    public ICliCommand Root { get; }

    public ICliCommand Parent { get; }

    //public Dictionary<string, ICliCommand> Ancestors { get; }

    public Dictionary<string, CliArgumentMetadata> Metadata { get; }

    //public Dictionary<string, ICliCommand> Descendants { get; }

    public void Execute();
}

public abstract class CliCommandBase<T> : ICliCommand
{
    public CliCommandBase(ICliCommand root, ICliCommand parent)
    {
        Root = root;
        Parent = parent;
        //Ancestors = [];
        //var validProperties = typeof(T)
        //    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        //    // Properties must be both set and get properties for writing data and reading for Execute.
        //    .Where(pi => pi.HasPublicSetAndGet())
        //    // Key for this group is true if it is a sub-command, false otherwise.
        //    // https://stackoverflow.com/a/4963190/294804
        //    .GroupBy(pi => pi.PropertyType.GetInterface(nameof(ICliCommand)) != null)
        //    .ToArray();

        Metadata = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            // Properties must be both set and get properties for writing data and reading for Execute and JSON deserialization.
            // Command properties only use the Set for JSON deserialization.
            .Where(pi => pi.HasPublicSetAndGet())
            // TODO: Check to see that CliArgumentAccessType.NameOnly is used on booleans-only.
            .Select(pi => new CliArgumentMetadata(this, pi)
            {
                Name = pi.GetCliNameAttributeValue() ?? pi.Name,
                Aliases = pi.GetCliAliasAttributeValue() ?? [],
                AccessType = pi.GetCliAccessTypeAttributeValue() ?? CliArgumentAccessType.NameAndValue,
                IsRequired = pi.HasCliRequiredAttribute(),
                Position = pi.GetCliPositionAttributeValue() ?? null,
                // https://stackoverflow.com/a/4963190/294804
                IsCommand = pi.PropertyType.GetInterface(nameof(ICliCommand)) != null
            })
            .ToDictionary(cam => cam.Name);

        //Descendants = validProperties
        //    // Key true = sub-command
        //    .Where(g => g.Key)
        //    .SelectMany(g => g)
        //    .Select(pi => new CliArgumentMetadata
        //    {
        //        // TODO: More stuff goes here
        //        Name = pi.GetCliNameAttributeValue() ?? pi.Name
        //    })
        //    .ToDictionary(cam => cam.Name);
    }

    public ICliCommand Root { get; }

    //public Dictionary<string, ICliCommand> Ancestors { get; }

    public ICliCommand Parent { get; }

    public Dictionary<string, CliArgumentMetadata> Metadata { get; }

    //public Dictionary<string, ICliCommand> Descendants { get; }

    public abstract void Execute();
}

public class CliArgumentMetadata(ICliCommand command, PropertyInfo propertyInfo)
{
    private readonly ICliCommand _command = command;
    private readonly PropertyInfo _propertyInfo = propertyInfo;

    public string Name { get; set; }
    public string[] Aliases { get; set; }
    public CliArgumentAccessType AccessType { get; set; }
    public bool IsRequired { get; set; }
    public int? Position { get; set; }
    public bool IsCommand { get; set; }

    internal void SetPropertyValue(string value)
    {
        // TODO: This code needs a lot of fleshing out with type conversions.
        // TODO: Create type conversion attribute for complex types. (string to new instance of the type)
        _propertyInfo.SetValue(_command, Convert.ChangeType(value, _propertyInfo.PropertyType));
    }

    public object Value => _propertyInfo.GetValue(_command);

    internal bool HasNameOrAlias(string name) => Name == name || Aliases.Contains(name);
}

public enum CliArgumentAccessType
{
    // Default: Both the argument name and value are specified. (Named)
    NameAndValue,
    // Only the argument value is present. (Unnamed)
    ValueOnly,
    // Only the argument name is present. (Flag)
    NameOnly
}
