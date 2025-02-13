// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Microsoft.DotNet.Cli.Telemetry;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.CommandFactory;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.ShellShim;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using CommandResult = System.CommandLine.Parsing.CommandResult;
using LocalizableStrings = Microsoft.DotNet.Cli.Utils.LocalizableStrings;
using Microsoft.DotNet.Workloads.Workload;
using System.Text.Json;
using System.Reflection;

namespace Microsoft.DotNet.Cli
{
    public class Program
    {
        private static readonly string ToolPathSentinelFileName = $"{Product.Version}.toolpath.sentinel";

        public static int Main(string[] args)
        {
            using AutomaticEncodingRestorer _ = new();

            // Setting output encoding is not available on those platforms
            if (UILanguageOverride.OperatingSystemSupportsUtf8())
            {
                Console.OutputEncoding = Encoding.UTF8;
            }

            DebugHelper.HandleDebugSwitch(ref args);

            // Capture the current timestamp to calculate the host overhead.
            DateTime mainTimeStamp = DateTime.Now;
            TimeSpan startupTime = mainTimeStamp - Process.GetCurrentProcess().StartTime;

            bool perfLogEnabled = Env.GetEnvironmentVariableAsBool("DOTNET_CLI_PERF_LOG", false);

            if (string.IsNullOrEmpty(Env.GetEnvironmentVariable("MSBUILDFAILONDRIVEENUMERATINGWILDCARD")))
            {
                Environment.SetEnvironmentVariable("MSBUILDFAILONDRIVEENUMERATINGWILDCARD", "1");
            }

            // Avoid create temp directory with root permission and later prevent access in non sudo
            if (SudoEnvironmentDirectoryOverride.IsRunningUnderSudo())
            {
                perfLogEnabled = false;
            }

            PerformanceLogStartupInformation startupInfo = null;
            if (perfLogEnabled)
            {
                startupInfo = new PerformanceLogStartupInformation(mainTimeStamp);
                PerformanceLogManager.InitializeAndStartCleanup(FileSystemWrapper.Default);
            }

            PerformanceLogEventListener perLogEventListener = null;
            try
            {
                if (perfLogEnabled)
                {
                    perLogEventListener = PerformanceLogEventListener.Create(FileSystemWrapper.Default, PerformanceLogManager.Instance.CurrentLogDirectory);
                }

                PerformanceLogEventSource.Log.LogStartUpInformation(startupInfo);
                PerformanceLogEventSource.Log.CLIStart();

                InitializeProcess();

                try
                {
                    var options = new JsonWriterOptions { Indented = true };
                    using var stream = new MemoryStream();
                    using var writer = new Utf8JsonWriter(stream, options);

                    writer.WriteStartObject();

                    TraverseCli(Parser.Instance.RootCommand, writer);

                    writer.WriteEndObject();
                    writer.Flush();

                    string json = Encoding.UTF8.GetString(stream.ToArray());
                    Console.WriteLine(json);

                    return 0;
                    //return ProcessArgs(args, startupTime);
                }
                catch (Exception e) when (e.ShouldBeDisplayedAsError())
                {
                    Reporter.Error.WriteLine(CommandLoggingContext.IsVerbose
                        ? e.ToString().Red().Bold()
                        : e.Message.Red().Bold());

                    var commandParsingException = e as CommandParsingException;
                    if (commandParsingException != null && commandParsingException.ParseResult != null)
                    {
                        commandParsingException.ParseResult.ShowHelp();
                    }

                    return 1;
                }
                catch (Exception e) when (!e.ShouldBeDisplayedAsError())
                {
                    // If telemetry object has not been initialized yet. It cannot be collected
                    TelemetryEventEntry.SendFiltered(e);
                    Reporter.Error.WriteLine(e.ToString().Red().Bold());

                    return 1;
                }
                finally
                {
                    PerformanceLogEventSource.Log.CLIStop();
                }
            }
            finally
            {
                if (perLogEventListener != null)
                {
                    perLogEventListener.Dispose();
                }
            }

            static void TraverseCli(CliCommand command, Utf8JsonWriter writer)
            {
                writer.WriteString("Primitive", typeof(CliCommand).FullName);
                writer.WriteString(nameof(command.Description), command.Description);
                writer.WriteBoolean(nameof(command.Hidden), command.Hidden);

                foreach (var symbol in command.Children)
                {
                    if (symbol is CliCommand subCommand)
                    {
                        writer.WriteStartObject(subCommand.Name);
                        TraverseCli(subCommand, writer);
                        writer.WriteEndObject();
                        continue;
                    }

                    if (symbol is CliOption option)
                    {
                        writer.WriteStartObject(option.Name);

                        writer.WriteString("Primitive", typeof(CliOption).FullName);
                        writer.WriteString(nameof(option.HelpName), option.HelpName);
                        writer.WriteString(nameof(option.Description), option.Description);

                        var internalArgument = typeof(CliOption).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic).First(pi => pi.Name == "Argument").GetValue(option) as CliArgument;
                        writer.WriteString(nameof(internalArgument.ValueType), internalArgument.ValueType.FullName);

                        writer.WriteStartArray(nameof(option.Aliases));
                        foreach(var alias in option.Aliases)
                        {
                            writer.WriteStringValue(alias);
                        }
                        writer.WriteEndArray();

                        writer.WriteStartObject(nameof(option.Arity));
                        writer.WriteNumber(nameof(option.Arity.MinimumNumberOfValues), option.Arity.MinimumNumberOfValues);
                        writer.WriteNumber(nameof(option.Arity.MaximumNumberOfValues), option.Arity.MaximumNumberOfValues);
                        writer.WriteEndObject();

                        writer.WriteBoolean(nameof(option.Required), option.Required);
                        writer.WriteBoolean(nameof(option.Hidden), option.Hidden);
                        writer.WriteBoolean(nameof(option.HasDefaultValue), option.HasDefaultValue);
                        writer.WriteBoolean(nameof(option.Recursive), option.Recursive);
                        writer.WriteBoolean(nameof(option.AllowMultipleArgumentsPerToken), option.AllowMultipleArgumentsPerToken);

                        writer.WriteEndObject();
                        continue;
                    }

                    if (symbol is CliArgument argument)
                    {
                        writer.WriteStartObject(argument.Name);

                        writer.WriteString("Primitive", typeof(CliArgument).FullName);
                        writer.WriteString(nameof(argument.HelpName), argument.HelpName);
                        writer.WriteString(nameof(argument.Description), argument.Description);
                        writer.WriteString(nameof(argument.ValueType), argument.ValueType.FullName);

                        writer.WriteStartObject(nameof(argument.Arity));
                        writer.WriteNumber(nameof(argument.Arity.MinimumNumberOfValues), argument.Arity.MinimumNumberOfValues);
                        writer.WriteNumber(nameof(argument.Arity.MaximumNumberOfValues), argument.Arity.MaximumNumberOfValues);
                        writer.WriteEndObject();

                        writer.WriteBoolean(nameof(argument.Hidden), argument.Hidden);
                        writer.WriteBoolean(nameof(argument.HasDefaultValue), argument.HasDefaultValue);

                        writer.WriteEndObject();
                        continue;
                    }
                }
            }
        }

        internal static int ProcessArgs(string[] args, ITelemetry telemetryClient = null)
        {
            return ProcessArgs(args, new TimeSpan(0), telemetryClient);
        }

        internal static int ProcessArgs(string[] args, TimeSpan startupTime, ITelemetry telemetryClient = null)
        {
            Dictionary<string, double> performanceData = new();

            PerformanceLogEventSource.Log.BuiltInCommandParserStart();
            ParseResult parseResult;
            using (new PerformanceMeasurement(performanceData, "Parse Time"))
            {
                parseResult = Parser.Instance.Parse(args);

                // Avoid create temp directory with root permission and later prevent access in non sudo
                // This method need to be run very early before temp folder get created
                // https://github.com/dotnet/sdk/issues/20195
                SudoEnvironmentDirectoryOverride.OverrideEnvironmentVariableToTmp(parseResult);
            }
            PerformanceLogEventSource.Log.BuiltInCommandParserStop();

            using (IFirstTimeUseNoticeSentinel disposableFirstTimeUseNoticeSentinel =
                new FirstTimeUseNoticeSentinel())
            {
                IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel = disposableFirstTimeUseNoticeSentinel;
                IAspNetCertificateSentinel aspNetCertificateSentinel = new AspNetCertificateSentinel();
                IFileSentinel toolPathSentinel = new FileSentinel(
                    new FilePath(
                        Path.Combine(
                            CliFolderPathCalculator.DotnetUserProfileFolderPath,
                            ToolPathSentinelFileName)));
                if (parseResult.GetValue(Parser.DiagOption) && parseResult.IsDotnetBuiltInCommand())
                {
                    // We found --diagnostic or -d, but we still need to determine whether the option should
                    // be attached to the dotnet command or the subcommand.
                    if (args.DiagOptionPrecedesSubcommand(parseResult.RootSubCommandResult()))
                    {
                        Environment.SetEnvironmentVariable(CommandLoggingContext.Variables.Verbose, bool.TrueString);
                        CommandLoggingContext.SetVerbose(true);
                        Reporter.Reset();
                    }
                }
                if (parseResult.HasOption(Parser.VersionOption) && parseResult.IsTopLevelDotnetCommand())
                {
                    CommandLineInfo.PrintVersion();
                    return 0;
                }
                else if (parseResult.HasOption(Parser.InfoOption) && parseResult.IsTopLevelDotnetCommand())
                {
                    CommandLineInfo.PrintInfo();
                    return 0;
                }
                else
                {
                    PerformanceLogEventSource.Log.FirstTimeConfigurationStart();

                    var environmentProvider = new EnvironmentProvider();

                    bool generateAspNetCertificate = environmentProvider.GetEnvironmentVariableAsBool(EnvironmentVariableNames.DOTNET_GENERATE_ASPNET_CERTIFICATE, defaultValue: true);
                    bool telemetryOptout = environmentProvider.GetEnvironmentVariableAsBool(EnvironmentVariableNames.TELEMETRY_OPTOUT, defaultValue: CompileOptions.TelemetryOptOutDefault);
                    bool addGlobalToolsToPath = environmentProvider.GetEnvironmentVariableAsBool(EnvironmentVariableNames.DOTNET_ADD_GLOBAL_TOOLS_TO_PATH, defaultValue: true);
                    bool nologo = environmentProvider.GetEnvironmentVariableAsBool(EnvironmentVariableNames.DOTNET_NOLOGO, defaultValue: false);
                    bool skipWorkloadIntegrityCheck = environmentProvider.GetEnvironmentVariableAsBool(EnvironmentVariableNames.DOTNET_SKIP_WORKLOAD_INTEGRITY_CHECK,
                        // Default the workload integrity check skip to true if the command is being ran in CI. Otherwise, false.
                        defaultValue: new CIEnvironmentDetectorForTelemetry().IsCIEnvironment());

                    ReportDotnetHomeUsage(environmentProvider);

                    var isDotnetBeingInvokedFromNativeInstaller = false;
                    if (parseResult.CommandResult.Command.Name.Equals(Parser.InstallSuccessCommand.Name))
                    {
                        aspNetCertificateSentinel = new NoOpAspNetCertificateSentinel();
                        firstTimeUseNoticeSentinel = new NoOpFirstTimeUseNoticeSentinel();
                        toolPathSentinel = new NoOpFileSentinel(exists: false);
                        isDotnetBeingInvokedFromNativeInstaller = true;
                    }

                    var dotnetFirstRunConfiguration = new DotnetFirstRunConfiguration(
                        generateAspNetCertificate: generateAspNetCertificate,
                        telemetryOptout: telemetryOptout,
                        addGlobalToolsToPath: addGlobalToolsToPath,
                        nologo: nologo,
                        skipWorkloadIntegrityCheck: skipWorkloadIntegrityCheck);

                    ConfigureDotNetForFirstTimeUse(
                        firstTimeUseNoticeSentinel,
                        aspNetCertificateSentinel,
                        toolPathSentinel,
                        isDotnetBeingInvokedFromNativeInstaller,
                        dotnetFirstRunConfiguration,
                        environmentProvider,
                        performanceData);
                    PerformanceLogEventSource.Log.FirstTimeConfigurationStop();
                }

                PerformanceLogEventSource.Log.TelemetryRegistrationStart();

                telemetryClient ??= new Telemetry.Telemetry(firstTimeUseNoticeSentinel);
                TelemetryEventEntry.Subscribe(telemetryClient.TrackEvent);
                TelemetryEventEntry.TelemetryFilter = new TelemetryFilter(Sha256Hasher.HashWithNormalizedCasing);

                PerformanceLogEventSource.Log.TelemetryRegistrationStop();
            }

            if (CommandLoggingContext.IsVerbose)
            {
                Console.WriteLine($"Telemetry is: {(telemetryClient.Enabled ? "Enabled" : "Disabled")}");
            }
            PerformanceLogEventSource.Log.TelemetrySaveIfEnabledStart();
            performanceData.Add("Startup Time", startupTime.TotalMilliseconds);
            TelemetryEventEntry.SendFiltered(Tuple.Create(parseResult, performanceData));
            PerformanceLogEventSource.Log.TelemetrySaveIfEnabledStop();

            int exitCode;
            if (parseResult.CanBeInvoked())
            {
                PerformanceLogEventSource.Log.BuiltInCommandStart();

                try
                {
                    exitCode = parseResult.Invoke();
                    exitCode = AdjustExitCode(parseResult, exitCode);
                }
                catch (Exception exception)
                {
                    exitCode = Parser.ExceptionHandler(exception, parseResult);
                }

                PerformanceLogEventSource.Log.BuiltInCommandStop();
            }
            else
            {
                PerformanceLogEventSource.Log.ExtensibleCommandResolverStart();
                try
                {
                    var resolvedCommand = CommandFactoryUsingResolver.Create(
                            "dotnet-" + parseResult.GetValue(Parser.DotnetSubCommand),
                            args.GetSubArguments(),
                            FrameworkConstants.CommonFrameworks.NetStandardApp15);
                    PerformanceLogEventSource.Log.ExtensibleCommandResolverStop();

                    PerformanceLogEventSource.Log.ExtensibleCommandStart();
                    var result = resolvedCommand.Execute();
                    PerformanceLogEventSource.Log.ExtensibleCommandStop();

                    exitCode = result.ExitCode;
                }
                catch (CommandUnknownException e)
                {
                    Reporter.Error.WriteLine(e.Message.Red());
                    Reporter.Output.WriteLine(e.InstructionMessage);
                    exitCode = 1;
                }
            }

            PerformanceLogEventSource.Log.TelemetryClientFlushStart();
            telemetryClient.Flush();
            PerformanceLogEventSource.Log.TelemetryClientFlushStop();

            telemetryClient.Dispose();

            return exitCode;
        }

        private static int AdjustExitCode(ParseResult parseResult, int exitCode)
        {
            if (parseResult.Errors.Count > 0)
            {
                var commandResult = parseResult.CommandResult;

                while (commandResult is not null)
                {
                    if (commandResult.Command.Name == "new")
                    {
                        // default parse error exit code is 1
                        // for the "new" command and its subcommands it needs to be 127
                        return 127;
                    }

                    commandResult = commandResult.Parent as CommandResult;
                }
            }

            return exitCode;
        }

        private static void ReportDotnetHomeUsage(IEnvironmentProvider provider)
        {
            var home = provider.GetEnvironmentVariable(CliFolderPathCalculator.DotnetHomeVariableName);
            if (string.IsNullOrEmpty(home))
            {
                return;
            }

            Reporter.Verbose.WriteLine(
                string.Format(
                    LocalizableStrings.DotnetCliHomeUsed,
                    home,
                    CliFolderPathCalculator.DotnetHomeVariableName));
        }

        private static void ConfigureDotNetForFirstTimeUse(
           IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel,
           IAspNetCertificateSentinel aspNetCertificateSentinel,
           IFileSentinel toolPathSentinel,
           bool isDotnetBeingInvokedFromNativeInstaller,
           DotnetFirstRunConfiguration dotnetFirstRunConfiguration,
           IEnvironmentProvider environmentProvider,
           Dictionary<string, double> performanceMeasurements)
        {
            var isFirstTimeUse = !firstTimeUseNoticeSentinel.Exists();
            var environmentPath = EnvironmentPathFactory.CreateEnvironmentPath(isDotnetBeingInvokedFromNativeInstaller, environmentProvider);
            var commandFactory = new DotNetCommandFactory(alwaysRunOutOfProc: true);
            var aspnetCertificateGenerator = new AspNetCoreCertificateGenerator();
            var reporter = Reporter.Output;
            var dotnetConfigurer = new DotnetFirstTimeUseConfigurer(
                firstTimeUseNoticeSentinel,
                aspNetCertificateSentinel,
                aspnetCertificateGenerator,
                toolPathSentinel,
                dotnetFirstRunConfiguration,
                reporter,
                environmentPath,
                performanceMeasurements);

            dotnetConfigurer.Configure();

            if (isDotnetBeingInvokedFromNativeInstaller && OperatingSystem.IsWindows())
            {
                DotDefaultPathCorrector.Correct();
            }

            if (isFirstTimeUse && !dotnetFirstRunConfiguration.SkipWorkloadIntegrityCheck)
            {
                try
                {
                    WorkloadIntegrityChecker.RunFirstUseCheck(reporter);
                }
                catch (Exception)
                {
                    // If the workload check fails for any reason, we want to eat the failure and continue running the command.
                    reporter.WriteLine(Workloads.Workload.LocalizableStrings.WorkloadIntegrityCheckError.Yellow());
                }
            }
        }

        private static void InitializeProcess()
        {
            // by default, .NET Core doesn't have all code pages needed for Console apps.
            // see the .NET Core Notes in https://docs.microsoft.com/dotnet/api/system.diagnostics.process#-notes
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            UILanguageOverride.Setup();
        }
    }
}
