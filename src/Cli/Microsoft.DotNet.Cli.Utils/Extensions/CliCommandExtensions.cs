using System.CommandLine;
using System.Reflection;

namespace Microsoft.DotNet.Cli.Utils.Extensions;

public static class CliCommandExtensions
{
    private static readonly PropertyInfo[] s_nonPublicProperties = typeof(CliCommand).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);

    public static bool? GetHasArguments(this CliCommand command) =>
        s_nonPublicProperties.First(pi => pi.Name == "HasArguments").GetValue(command) as bool?;

    public static bool? GetHasOptions(this CliCommand command) =>
        s_nonPublicProperties.First(pi => pi.Name == "HasOptions").GetValue(command) as bool?;

    public static bool? GetHasSubcommands(this CliCommand command) =>
        s_nonPublicProperties.First(pi => pi.Name == "HasSubcommands").GetValue(command) as bool?;

    public static bool? GetHasValidators(this CliCommand command) =>
        s_nonPublicProperties.First(pi => pi.Name == "HasValidators").GetValue(command) as bool?;
}
