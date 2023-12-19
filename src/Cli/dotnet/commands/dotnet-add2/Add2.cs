// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.CliSimplify;

namespace Microsoft.DotNet.Cli.commands.dotnet_add2;

public class DotNet : ICliCommand
{
    [CliPropertyName("add2")]
    public Add2 Add2 { get; set; }

    public void Execute() => throw new NotImplementedException();
}

public class Add2 : ICliCommand
{
    [CliPropertyName("project")]
    public string Project { get; set; }

    [CliPropertyName("package")]
    public AddPackage Package { get; set; }

    [CliPropertyName("reference")]
    public AddReference Reference { get; set; }

    public void Execute() => throw new NotImplementedException();

    //public string Parse(string[] args)
    //{
    //    var sb = new StringBuilder();
    //    for (int i = 0; i < args.Length; i++)
    //    {
    //        switch (i)
    //        {
    //            case 0:
    //                switch (args[i])
    //                {
    //                    case "package":
    //                    case "reference":
    //                        sb.Append($"\"{args[i]}\": {{");
    //                        break;
    //                    default:

    //                        break;
    //                }
    //                break;
    //            case 1:
    //                break;
    //            case 2:
    //                break;
    //        }
    //    }
    //}
}

public class AddPackage : ICliCommand
{
    [CliPropertyName("name")]
    public string Name { get; set; }

    [CliPropertyName("version")]
    public Version Version { get; set; }

    public void Execute()
    {

    }
}

public class AddReference : ICliCommand
{
    public void Execute()
    {

    }
}
