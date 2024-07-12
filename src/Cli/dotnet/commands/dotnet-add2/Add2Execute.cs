// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Tools;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;
using NuGet.Frameworks;
using Microsoft.DotNet.Tools.MSBuild;
using Microsoft.DotNet.Tools.NuGet;

namespace Microsoft.DotNet.Cli.commands.dotnet_add2
{
    internal static class Add2Execute
    {
        public static void AddPackageExecute(AddPackage command)
        {
            var projectPath = (command.Parent as Add2)?.Project;
            if (!File.Exists(projectPath))
            {
                projectPath = MsbuildProject.GetProjectFileFromDirectory(projectPath).FullName;
            }

            var tempDgPath = string.Empty;
            if (!command.IsNoRestore)
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

            NuGetCommand.Run(TransformArgs(command.Name, projectPath, tempDgPath, command.IsNoRestore));

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

        public static void AddReferenceExecute(AddReference command)
        {
            var projectPath = (command.Parent as Add2)?.Project;
            var projects = new ProjectCollection();
            var project = MsbuildProject.FromFileOrDirectory(projects, projectPath, command.IsInteractive);

            // TODO: The property will eventually be References as a collection already.
            string[] references = [command.Reference];
            PathUtility.EnsureAllPathsExist(references, CommonLocalizableStrings.CouldNotFindProjectOrDirectory, true);
            var projectReferences = references.Select((r) => MsbuildProject.FromFileOrDirectory(projects, r, command.IsInteractive));

            // TODO: This is currently erroring when evaluating the project:
            // Project `C:\Workspace\Projects\ConsoleApp9\ConsoleApp9.csproj` could not be evaluated. Evaluation failed with following error:
            // The SDK 'Microsoft.NET.Sdk' specified could not be found.C:\Workspace\Projects\ConsoleApp9\ConsoleApp9.csproj.
            var frameworks = project.GetTargetFrameworks();
            if (!string.IsNullOrEmpty(command.Framework))
            {
                var providedFramework = NuGetFramework.Parse(command.Framework);
                if (!frameworks.Contains(providedFramework))
                {
                    Reporter.Error.WriteLine(CommonLocalizableStrings.ProjectDoesNotTargetFramework, project.ProjectRootElement.FullPath, command.Framework);
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
            int referenceAddedCount = project.AddProjectToProjectReferences(command.Framework, relativePathReferences);
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
}
