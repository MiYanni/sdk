// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.DotNet.Cli.CliSimplify;

internal class CliRegistry
{
    //private static CliRegistry s_instance;
    //public static CliRegistry Instance => s_instance ??= new CliRegistry();

    public CliRegistry(ICliCommand root)
    {
        _registry = root.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            // Commands must be both set and get properties for writing data and reading into JSON.
            // https://stackoverflow.com/a/4963190/294804
            .Where(pi => pi.PropertyType.GetInterface(nameof(ICliCommand)) != null && pi.HasPublicSetAndGet())
            // TODO: Use attribute for CliName
            .ToDictionary(pi => GetCliPropertyNameAttributeValue(pi) ?? pi.Name);
    }

    private static string GetCliPropertyNameAttributeValue(PropertyInfo propertyInfo) =>
        (propertyInfo.GetCustomAttribute(typeof(CliPropertyNameAttribute), true) as CliPropertyNameAttribute)?.Name;

    private readonly Dictionary<string, PropertyInfo> _registry;

    public void AddCommand(ICliCommand command) => _registry.Add(command.Name, command);

    // TODO: Logic/options for conflict resolution
    //public bool HasCommand(string commandName)

    // Fast path (normal commandName is used). Slow path (command alias is used).
    public ICliCommand GetCommandOrDefault(string commandName) => _registry.TryGetValue(commandName, out ICliCommand command) ? command : GetByAliasOrDefault(commandName);

    private ICliCommand GetByAliasOrDefault(string aliasName) => _registry.Values.FirstOrDefault(c => c.Aliases.Contains(aliasName));
}
