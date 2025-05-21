// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli.Utils.Api;

namespace Microsoft.DotNet.Cli.Commands.Solution.List;

public class SolutionListCmd(ICliCommand root, ICliCommand parent) : CliCommand<SolutionListCmd>(root, parent, "list", CliCommandStrings.ListAppFullName)
{
    [CliName("--solution-folders")]
    [CliDescription(typeof(CliCommandStrings), nameof(CliCommandStrings.ListSolutionFoldersArgumentDescription))]
    public bool DisplaySolutionFolders { get; set; }

    public override void Execute()
    {
        var solutionFile = (Parent as SolutionCmd)?.SolutionFile;
        // TODO: Figure out default value via S.CL factory.
        if (string.IsNullOrEmpty(solutionFile))
        {
            solutionFile = PathUtility.EnsureTrailingSlash(Directory.GetCurrentDirectory());
        }
        string solutionPath = SlnFileFactory.GetSolutionFileFullPath(solutionFile, includeSolutionFilterFiles: true);
        try
        {
            var solution = SlnFileFactory.CreateFromFileOrDirectory(solutionPath);
            string[] paths = DisplaySolutionFolders ?
                // VS-SolutionPersistence does not return a path object, so there might be issues with forward/backward slashes on different platforms
                [.. solution.SolutionFolders.Select(folder => Path.GetDirectoryName(folder.Path.TrimStart('/')))] :
                [.. solution.SolutionProjects.Select(project => project.FilePath)];

            if (!paths.Any())
            {
                Reporter.Output.WriteLine(CliStrings.NoProjectsFound);
                return;
            }

            var header = DisplaySolutionFolders ? CliCommandStrings.SolutionFolderHeader : CliCommandStrings.ProjectsHeader;
            Reporter.Output.WriteLine(header);
            Reporter.Output.WriteLine(new string('-', header.Length));

            foreach (string path in paths.Order())
            {
                Reporter.Output.WriteLine(path);
            }
        }
        catch (Exception ex)
        {
            throw new GracefulException(CliStrings.InvalidSolutionFormatString, solutionPath, ex.Message);
        }
    }
}
