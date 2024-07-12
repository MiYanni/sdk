// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.CliSimplify;

namespace Microsoft.DotNet.Cli.commands.dotnet_add2;

public class DotNet : CliCommandBase<DotNet>
{
    public DotNet() : base(null, null)
    {
        // TODO: Find a cleaner way to do this, hopefully.
        Add2 = new Add2(this, this);
    }

    [CliName("add2")]
    public Add2 Add2 { get; set; }

    public override void Execute() => throw new NotImplementedException();
}

public class Add2 : CliCommandBase<Add2>
{
    public Add2(ICliCommand root, ICliCommand parent) : base(root, parent)
    {
        Package = new AddPackage(root, this);
        Reference = new AddReference(root, this);
    }

    [CliAccessType(CliArgumentAccessType.ValueOnly)]
    public string Project { get; set; }

    // SUBCOMMANDS
    [CliName("package")]
    public AddPackage Package { get; set; }
    [CliName("reference")]
    public AddReference Reference { get; set; }

    public override void Execute() => throw new NotImplementedException();
}

public class AddPackage(ICliCommand root, ICliCommand parent) : CliCommandBase<AddPackage>(root, parent)
{
    [CliAccessType(CliArgumentAccessType.ValueOnly)]
    [CliRequired]
    public string Name { get; set; }

    [CliName("--framework")]
    [CliAlias("-f")]
    public string Framework { get; set; }

    [CliName("--interactive")]
    public bool IsInteractive { get; set; }

    [CliName("--no-restore")]
    [CliAlias("-n")]
    public bool IsNoRestore { get; set; }

    [CliName("--package-directory")]
    public string PackageDirectory { get; set; }

    [CliName("--prerelease")]
    public bool IsPrerelease { get; set; }

    [CliName("--source")]
    [CliAlias("-s")]
    public string Source { get; set; }

    // TODO: Make this Version type. Using string so casting doesn't explode.
    [CliName("--version")]
    [CliAlias("-v")]
    public string Version { get; set; }

    public override void Execute()
    {
        Add2Execute.AddPackageExecute(this);
    }
}

public class AddReference(ICliCommand root, ICliCommand parent) : CliCommandBase<AddReference>(root, parent)
{
    [CliAccessType(CliArgumentAccessType.ValueOnly)]
    [CliRequired]
    // TODO: This needs to be variadic (collection). Not supported yet.
    public string Reference { get; set; }

    [CliName("--framework")]
    [CliAlias("-f")]
    public string Framework { get; set; }

    [CliName("--interactive")]
    public bool IsInteractive { get; set; }

    public override void Execute()
    {
        Add2Execute.AddReferenceExecute(this);
    }
}
