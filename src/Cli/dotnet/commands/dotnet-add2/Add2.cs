// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.CliSimplify;

namespace Microsoft.DotNet.Cli.commands.dotnet_add2;

public class DotNet : CliCommandBase<DotNet>
{
    public DotNet() : base(null)
    {
        // TODO: Find a cleaner way to do this, hopefully.
        Add2 = new Add2(this);
    }

    [CliName("add2")]
    public Add2 Add2 { get; set; }

    public override void Execute() => throw new NotImplementedException();
}

public class Add2(ICliCommand root) : CliCommandBase<Add2>(root)
{
    [CliAccessType(CliArgumentAccessType.ValueOnly)]
    public string Project { get; set; }

    [CliName("package")]
    public AddPackage Package { get; set; } = new AddPackage(root);

    [CliName("reference")]
    public AddReference Reference { get; set; } = new AddReference(root);

    public override void Execute() => throw new NotImplementedException();
}

public class AddPackage(ICliCommand root) : CliCommandBase<AddPackage>(root)
{
    [CliAccessType(CliArgumentAccessType.ValueOnly)]
    [CliRequired]
    public string Name { get; set; }

    [CliName("--package-directory")]
    public string PackageDirectory { get; set; }

    [CliName("--version")]
    [CliAlias("-v")]
    public Version Version { get; set; }

    public override void Execute()
    {

    }
}

public class AddReference(ICliCommand root) : CliCommandBase<AddReference>(root)
{
    public override void Execute()
    {

    }
}
