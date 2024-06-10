// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.CliSimplify;
using Microsoft.DotNet.Tools;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;
using NuGet.Frameworks;
using Microsoft.DotNet.Tools.MSBuild;
using Microsoft.DotNet.Tools.NuGet;
using Microsoft.DotNet.CommandFactory;

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
        var projectPath = (Parent as Add2)?.Project;
        if (!File.Exists(projectPath))
        {
            projectPath = MsbuildProject.GetProjectFileFromDirectory(projectPath).FullName;
        }

        var tempDgPath = string.Empty;
        if (!IsNoRestore)
        {
            try
            {
                // Create a Dependency Graph file for the project
                tempDgPath = Path.GetTempFileName();
            }
            catch (IOException ioEx)
            {
                // Catch IOException from Path.GetTempFileName() and throw a graceful exception to the user.
                throw new GracefulException(string.Format(Tools.Add.PackageReference.LocalizableStrings.CmdDGFileIOException, projectPath), ioEx);
            }

            GetProjectDependencyGraph(projectPath, tempDgPath);
        }

        NuGetCommand.Run(TransformArgs(Name, projectPath, tempDgPath, IsNoRestore));

        if (File.Exists(tempDgPath))
        {
            File.Delete(tempDgPath);
        }
    }

    private static void GetProjectDependencyGraph(string projectFilePath, string dgFilePath)
    {
        var args = new string[]
        {
            // Pass the project file path
            projectFilePath,
            // Pass the task as generate restore Dependency Graph file
            "-target:GenerateRestoreGraphFile",
            // Pass Dependency Graph file output path
            $"-property:RestoreGraphOutputPath=\"{dgFilePath}\"",
            // Turn off recursive restore
            $"-property:RestoreRecursive=false",
            // Turn off restore for Dotnet cli tool references so that we do not generate extra dg specs
            $"-property:RestoreDotnetCliToolReferences=false",
            // Output should not include MSBuild version header
            "-nologo"
        };

        // TODO: Currently errors here:
        // Unable to create dependency graph file for project 'C:\Workspace\Projects\ConsoleApp9\ConsoleApp9.csproj'. Cannot add package reference.
        var result = new MSBuildForwardingApp(args).Execute();
        if (result != 0)
        {
            throw new GracefulException(string.Format(Tools.Add.PackageReference.LocalizableStrings.CmdDGFileException, projectFilePath));
        }
    }

    private static string[] TransformArgs(string packageId, string projectFilePath, string tempDgPath, bool isNoRestore) =>
    [
        "package",
        "add",
        "--package",
        packageId,
        "--project",
        projectFilePath,
        // TODO: Need a way to allow for forwarding arguments from the global space.
        //.. _parseResult.OptionValuesToBeForwarded(AddPackageParser.GetCommand()).SelectMany(a => a.Split(' ', 2)),
        .. !string.IsNullOrEmpty(tempDgPath) ? new string[] { "--dg-file", tempDgPath } : [],
        .. isNoRestore ? new string[] { "--no-restore" } : [],
    ];
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
        var projectPath = (Parent as Add2)?.Project;
        var projects = new ProjectCollection();
        var project = MsbuildProject.FromFileOrDirectory(projects, projectPath, IsInteractive);

        // TODO: The property will eventually be References as a collection already.
        string[] references = [Reference];
        PathUtility.EnsureAllPathsExist(references, CommonLocalizableStrings.CouldNotFindProjectOrDirectory, true);
        var projectReferences = references.Select((r) => MsbuildProject.FromFileOrDirectory(projects, r, IsInteractive));

        // TODO: This is currently erroring when evaluating the project:
        // Project `C:\Workspace\Projects\ConsoleApp9\ConsoleApp9.csproj` could not be evaluated. Evaluation failed with following error:
        // The SDK 'Microsoft.NET.Sdk' specified could not be found.C:\Workspace\Projects\ConsoleApp9\ConsoleApp9.csproj.
        var frameworks = project.GetTargetFrameworks();
        if (!string.IsNullOrEmpty(Framework))
        {
            var providedFramework = NuGetFramework.Parse(Framework);
            if (!frameworks.Contains(providedFramework))
            {
                Reporter.Error.WriteLine(CommonLocalizableStrings.ProjectDoesNotTargetFramework, project.ProjectRootElement.FullPath, Framework);
                return;
            }
            frameworks = [providedFramework];
        }

        foreach (var tfm in frameworks)
        {
            foreach (var projectReference in projectReferences)
            {
                if (!projectReference.CanWorkOnFramework(tfm))
                {
                    Reporter.Error.Write(GetProjectNotCompatibleWithFrameworksDisplayString(projectReference, frameworks.Select(fx => fx.GetShortFolderName())));
                    return;
                }
            }
        }

        var relativePathReferences = projectReferences.Select((r) => Path.GetRelativePath(project.ProjectDirectory, r.ProjectRootElement.FullPath));
        int referenceAddedCount = project.AddProjectToProjectReferences(Framework, relativePathReferences);
        if (referenceAddedCount != 0)
        {
            project.ProjectRootElement.Save();
        }
    }

    private static string GetProjectNotCompatibleWithFrameworksDisplayString(MsbuildProject project, IEnumerable<string> frameworksDisplayStrings)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Format(CommonLocalizableStrings.ProjectNotCompatibleWithFrameworks, project.ProjectRootElement.FullPath));
        foreach (var tfm in frameworksDisplayStrings)
        {
            sb.AppendLine($"    - {tfm}");
        }

        return sb.ToString();
    }
}
