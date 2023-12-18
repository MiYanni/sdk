// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.DotNet.Cli.CliSimplify;

internal class CliRegistry
{
    //private static CliRegistry s_instance;
    //public static CliRegistry Instance => s_instance ??= new CliRegistry();

    public CliRegistry(ICommand root)
    {
        _registry = root.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            // Commands must be both set and get properties for writing data and reading into JSON.
            .Where(pi => pi.PropertyType == typeof(ICommand) && pi.HasPublicSetAndGet())
            .ToDictionary(pi => pi.Name);
    }

    private readonly Dictionary<string, PropertyInfo> _registry;

    public void AddCommand(ICommand command) => _registry.Add(command.Name, command);

    // TODO: Logic/options for conflict resolution
    //public bool HasCommand(string commandName)

    // Fast path (normal commandName is used). Slow path (command alias is used).
    public ICommand GetCommandOrDefault(string commandName) => _registry.TryGetValue(commandName, out ICommand command) ? command : GetByAliasOrDefault(commandName);

    private ICommand GetByAliasOrDefault(string aliasName) => _registry.Values.FirstOrDefault(c => c.Aliases.Contains(aliasName));
}
