// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.DotNet.Cli.CliSimplify;

public interface ICliCommand
{
    public Dictionary<string, ICliCommand> Ancestors { get; }

    public Dictionary<string, CliArgumentMetadata> Metadata { get; }

    //public Dictionary<string, ICliCommand> Descendants { get; }

    public void Execute();
}

internal abstract class CliCommandBase<T> : ICliCommand
{
    public CliCommandBase()
    {
        Ancestors = [];
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
            // Properties must be both set and get properties for writing data and reading for Execute.
            .Where(pi => pi.HasPublicSetAndGet())
            .Select(pi => new CliArgumentMetadata
            {
                // TODO: More stuff goes here
                Name = pi.GetCliPropertyNameAttributeValue() ?? pi.Name,
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
        //        Name = pi.GetCliPropertyNameAttributeValue() ?? pi.Name
        //    })
        //    .ToDictionary(cam => cam.Name);
    }

    public Dictionary<string, ICliCommand> Ancestors { get; }

    public Dictionary<string, CliArgumentMetadata> Metadata { get; }

    //public Dictionary<string, ICliCommand> Descendants { get; }

    public abstract void Execute();
}

public class CliArgumentMetadata
{
    public string Name { get; set; }
    public string[] Aliases { get; set; }
    public bool IsRequired { get; set; }
    public bool IsFlag { get; set; }
    public bool IsCommand { get; set; }
    public int? Position { get; set; }
    public object Value { get; set; }
}
