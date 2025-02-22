using System.CommandLine;
using System.Reflection;

namespace Microsoft.DotNet.Cli.Utils.Extensions;

public static class CliOptionExtensions
{
    private static readonly PropertyInfo[] s_nonPublicProperties = typeof(CliOption).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);

    public static CliArgument? GetArgument(this CliOption option) =>
        s_nonPublicProperties.First(pi => pi.Name == "Argument").GetValue(option) as CliArgument;

    public static bool? GetHasValidators(this CliOption option) =>
        s_nonPublicProperties.First(pi => pi.Name == "HasValidators").GetValue(option) as bool?;
}
