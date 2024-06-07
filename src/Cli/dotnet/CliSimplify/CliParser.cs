// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Cli.CliSimplify;

internal static class CliParser
{
    // TODO: This code does not function with variadic value arguments.
    public static ICliCommand Parse(ICliCommand root, string[] arguments)
    {
        var currentCommand = root;
        // TODO: Required are processed as a separate set from positional. Should this be the design? Consider a scenario with:
        // Pos1: R, Pos2: O, Pos3: R
        // Should that be allowed or not?
        // TODO: Should a Required command be allowed???
        var requiredArguments = new Queue<CliArgumentMetadata>(currentCommand.Metadata.Values.Where(m => m.IsRequired && !m.IsCommand).OrderBy(m => m.Position));
        CliArgumentMetadata activeArgument = null;
        //bool isSatisfied = false;
        foreach (var argument in arguments)
        {
            var isActive = activeArgument != null;
            if (requiredArguments.Any() || activeArgument?.IsRequired == true)
            {
                var requiredArgument = activeArgument ?? requiredArguments.Dequeue();
                if (requiredArgument.AccessType == CliArgumentAccessType.ValueOnly)
                {
                    requiredArgument.SetPropertyValue(argument);
                    continue;
                }

                if (isActive && requiredArgument.AccessType == CliArgumentAccessType.NameAndValue)
                {
                    requiredArgument.SetPropertyValue(argument);
                    activeArgument = null;
                    continue;
                }

                if (requiredArgument.AccessType == CliArgumentAccessType.NameOnly && requiredArgument.HasNameOrAlias(argument))
                {
                    requiredArgument.SetPropertyValue(bool.TrueString);
                    continue;
                }

                if (requiredArgument.AccessType == CliArgumentAccessType.NameAndValue && requiredArgument.HasNameOrAlias(argument))
                {
                    activeArgument = requiredArgument;
                    continue;
                }

                throw new ArgumentException("Invalid required argument specified.");
            }

            // TODO: Construct Metadata so that all entries for both name and alias exists in the dictionary. Basically, multiple entries reference the same value instance. Then, it will always be fast lookup.
            // Fast lookup via dictionary key
            if (!isActive && (currentCommand.Metadata.TryGetValue(argument, out CliArgumentMetadata metadata) ||
                // Slow lookup via iteration of name + aliases
                (metadata = currentCommand.Metadata.Values.FirstOrDefault(m => m.HasNameOrAlias(argument))) != null))
            {
                if (metadata.IsCommand)
                {
                    if (requiredArguments.Any())
                    {
                        throw new ArgumentException($"Not all required arguments have been specified for the command {argument}");
                    }

                    currentCommand = (ICliCommand)metadata.Value;
                    requiredArguments = new Queue<CliArgumentMetadata>(currentCommand.Metadata.Values.Where(m => m.IsRequired && !m.IsCommand).OrderBy(m => m.Position));
                    continue;
                }

                if (metadata.AccessType == CliArgumentAccessType.NameAndValue)
                {
                    activeArgument = metadata;
                    continue;
                }

                if (metadata.AccessType == CliArgumentAccessType.NameOnly)
                {
                    metadata.SetPropertyValue(bool.TrueString);
                    continue;
                }

                throw new ArgumentException("Invalid argument type specified.");
            }

            if (isActive)
            {
                activeArgument.SetPropertyValue(argument);
                activeArgument = null;
                continue;
            }

            // TODO: This doesn't handle positionality or multiple optional value-only arguments.
            var optionalValueOnly = currentCommand.Metadata.Values.FirstOrDefault(m => !m.IsRequired && !m.IsCommand && m.AccessType == CliArgumentAccessType.ValueOnly);
            if (optionalValueOnly != null)
            {
                optionalValueOnly.SetPropertyValue(argument);
                continue;
            }

            throw new ArgumentException("Invalid argument specified.");
        }

        if (requiredArguments.Any())
        {
            throw new ArgumentException("Missing required arguments.");
        }

        return currentCommand;
    }
}
