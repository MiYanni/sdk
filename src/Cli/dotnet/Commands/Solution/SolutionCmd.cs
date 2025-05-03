// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Commands.Solution.List;
using Microsoft.DotNet.Cli.Utils.Api;

namespace Microsoft.DotNet.Cli.Commands.Solution;

public class SolutionCmd : CliCommand<SolutionCmd>
{
    public SolutionCmd(ICliCommand root, ICliCommand parent) : base(root, parent, "solution", CliCommandStrings.SolutionAppFullName)
    {
        // TODO: This needs generic processing via attributes.
        Aliases.Add("sln");
        // TODO: This needs generic processing via attributes.
        Subcommands.Add(new SolutionListCmd(root, parent));
    }

    // TODO: This is currently unused but will be used for determining arity.
    [CliAccessType(CliArgumentAccessType.ValueOnly)]
    // TODO: This actually comes from a resource for this argument. Need to determine how to handle properly.
    [CliName("SLN_FILE")]
    [CliDescription(typeof(CliCommandStrings), nameof(CliCommandStrings.SolutionArgumentDescription))]
    public string SolutionFile { get; set; }

    //private static object SolutionFileDefaultValue() => PathUtility.EnsureTrailingSlash(Directory.GetCurrentDirectory());

    public override void Execute()
    {
        // TODO: This actually runs logic for HandleMissingCommand but that requires ParseResult, so that needs to be implemented generically in the CliCommand class.
    }
}
