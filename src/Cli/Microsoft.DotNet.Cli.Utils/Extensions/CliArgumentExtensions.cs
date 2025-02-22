using System.CommandLine;
using System.Reflection;

namespace Microsoft.DotNet.Cli.Utils.Extensions;

public static class CliArgumentExtensions
{
    private static readonly PropertyInfo[] s_nonPublicProperties = typeof(CliArgument).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);

    public static bool? GetHasValidators(this CliArgument argument) =>
        s_nonPublicProperties.First(pi => pi.Name == "HasValidators").GetValue(argument) as bool?;
}
