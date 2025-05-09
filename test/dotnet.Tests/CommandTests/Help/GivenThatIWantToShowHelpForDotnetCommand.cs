﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Commands;
using Microsoft.DotNet.Cli.Commands.Help;

namespace Microsoft.DotNet.Help.Tests
{
    public class GivenThatIWantToShowHelpForDotnetCommand : SdkTest
    {
        private const string HelpText =
@"Usage: dotnet [sdk-options] [command] [command-options] [arguments]

Execute a .NET SDK command.

sdk-options:
  -d|--diagnostics  Enable diagnostic output.
  -h|--help         Show command line help.
  --info            Display .NET information.
  --list-runtimes   Display the installed runtimes.
  --list-sdks       Display the installed SDKs.
  --version         Display .NET SDK version in use.

SDK commands:
  build             Build a .NET project.
  build-server      Interact with servers started by a build.
  clean             Clean build outputs of a .NET project.
  format            Apply style preferences to a project or solution.
  help              Opens the reference page in a browser for the specified command.
  msbuild           Run Microsoft Build Engine (MSBuild) commands.
  new               Create a new .NET project or file.
  nuget             Provides additional NuGet commands.
  pack              Create a NuGet package.
  package           Search for, add, remove, or list PackageReferences for a .NET project.
  publish           Publish a .NET project for deployment.
  reference         Add, remove, or list ProjectReferences for a .NET project.
  restore           Restore dependencies specified in a .NET project.
  run               Build and run a .NET project output.
  sdk               Manage .NET SDK installation.
  solution          Modify Visual Studio solution files.
  store             Store the specified assemblies in the runtime package store.
  test              Run unit tests using the test runner specified in a .NET project.
  tool              Install or manage tools that extend the .NET experience.
  vstest            Run Microsoft Test Engine (VSTest) commands.
  workload          Manage optional workloads.

Additional commands from bundled tools:
  dev-certs         Create and manage development certificates.
  fsi               Start F# Interactive / execute F# scripts.
  user-jwts         Manage JSON Web Tokens in development.
  user-secrets      Manage development user secrets.
  watch             Start a file watcher that runs a command when files change.

Run 'dotnet [command] --help' for more information on a command.";

        public GivenThatIWantToShowHelpForDotnetCommand(ITestOutputHelper log) : base(log)
        {
        }

        [Theory]
        [InlineData("--help")]
        [InlineData("-h")]
        [InlineData("-?")]
        [InlineData("/?")]
        public void WhenHelpOptionIsPassedToDotnetItPrintsUsage(string helpArg)
        {
            var cmd = new DotnetCommand(Log)
                .Execute(helpArg);
            cmd.Should().Pass();
            cmd.StdOut.Should().ContainVisuallySameFragmentIfNotLocalized(HelpText);
        }

        [Fact]
        public void WhenHelpCommandIsPassedToDotnetItPrintsUsage()
        {
            var cmd = new DotnetCommand(Log, "help")
                .Execute();
            cmd.Should().Pass();
            cmd.StdOut.Should().ContainVisuallySameFragmentIfNotLocalized(HelpText);
        }

        [Fact]
        public void WhenInvalidCommandIsPassedToDotnetHelpItPrintsError()
        {
            var cmd = new DotnetCommand(Log)
                  .Execute("help", "invalid");

            cmd.Should().Fail();
            cmd.StdErr.Should().Contain(string.Format(CliCommandStrings.CommandDoesNotExist, "invalid"));
            cmd.StdOut.Should().ContainVisuallySameFragmentIfNotLocalized(HelpText);
        }

        [Theory]
        [InlineData("complete")]
        [InlineData("parse")]
        public void WhenCommandWithoutDocLinkIsPassedToDotnetHelpItPrintsError(string command)
        {
            var cmd = new DotnetCommand(Log)
                  .Execute($"help", command);

            cmd.Should().Fail();
            cmd.StdErr.Should().Contain(string.Format(CliCommandStrings.CommandDoesNotExist, command));
            cmd.StdOut.Should().ContainVisuallySameFragmentIfNotLocalized(HelpText);
        }

        [WindowsOnlyFact]
        public void WhenRunOnWindowsDotnetHelpCommandShouldContainProperProcessInformation()
        {
            var proc = HelpCommand.ConfigureProcess("https://aka.ms/dotnet-build");
            Assert.EndsWith("cmd.exe", proc.StartInfo.FileName);
            Assert.Equal("/c start https://aka.ms/dotnet-build", proc.StartInfo.Arguments);
        }

        [LinuxOnlyFact]
        public void WhenRunOnLinuxDotnetHelpCommandShouldContainProperProcessInformation()
        {
            var proc = HelpCommand.ConfigureProcess("https://aka.ms/dotnet-build");
            Assert.Contains("xdg-open", proc.StartInfo.FileName);
            Assert.Equal("https://aka.ms/dotnet-build", proc.StartInfo.Arguments);

        }
        [MacOsOnlyFact]
        public void WhenRunOnMacOsDotnetHelpCommandShouldContainProperProcessInformation()
        {
            var proc = HelpCommand.ConfigureProcess("https://aka.ms/dotnet-build");
            Assert.EndsWith("open", proc.StartInfo.FileName);
            Assert.Equal("https://aka.ms/dotnet-build", proc.StartInfo.Arguments);
        }
    }
}
