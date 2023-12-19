// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Cli.CliSimplify;

public interface ICommand
{
    // Must be unique among all commands in the registry, since it is used as a Dictionary key.
    //public string Name { get; }

    //public IEnumerable<string> Aliases { get; }

    public void Execute();
}
