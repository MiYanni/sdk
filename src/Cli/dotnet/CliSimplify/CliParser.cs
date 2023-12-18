// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.DotNet.Cli.CliSimplify;

internal static class CliParser
{
    public static ICommand ParseOrDefault(ICommand root, IEnumerable<string> arguments)
    {
        //var commandName = arguments
        var command = CliRegistry.Instance.GetCommandOrDefault(arguments.FirstOrDefault()) ??
            throw new ArgumentException("Invalid command");
        // https://stackoverflow.com/a/824854/294804
        var memberNames = command.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            // Properties must be both set and get properties for writing data and reading into JSON.
            .Where(pi => pi.CanRead && pi.CanWrite && pi.GetGetMethod(false) != null && pi.GetSetMethod(false) != null)
            .ToDictionary(pi => pi.PropertyType)

    }

}
