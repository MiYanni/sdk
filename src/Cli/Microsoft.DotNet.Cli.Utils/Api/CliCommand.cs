// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Reflection;
using SclCommand = System.CommandLine.Command;

namespace Microsoft.DotNet.Cli.Utils.Api;

public interface ICliCommand
{
    public ICliCommand Root { get; }
    public ICliCommand Parent { get; }
    public void Execute();
}

public abstract class CliCommand<T> : SclCommand, ICliCommand
{
    private readonly PropertyInfo[] _properties = [.. typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        // Properties must be both set and get properties for writing data and reading for Execute and JSON deserialization.
        // Command properties only use the Set for JSON deserialization.
        .Where(pi => pi.HasPublicSetAndGet())];

    private static readonly Type s_optionGenericType = typeof(CliOption<>);
    private static readonly Type s_argumentGenericType = typeof(CliArgument<>);

    public CliCommand(ICliCommand root, ICliCommand parent, string name, string? description = null) : base(name, description)
    {
        Root = root;
        Parent = parent;
        //Options.AddRange(_properties.Select(pi =>
        //{
        //    var name = pi.GetCliNameAttributeValue() ?? pi.Name;
        //    var aliases = pi.GetCliAliasAttributeValue() ?? [];
        //    var description = pi.GetCliDescriptionAttributeValue() ?? string.Empty;
        //    // TODO: Expand all supported types and argument/option combinations
        //    if (pi.PropertyType == typeof(bool))
        //    {
        //        var boolOptionType = s_optionGenericType.MakeGenericType(typeof(bool));
        //        var boolOption = Activator.CreateInstance(boolOptionType, pi, name, aliases) as CliOption<bool>;
        //        boolOption?.Description = description;
        //        boolOption?.Arity = ArgumentArity.Zero;
        //        return boolOption ?? OptionHelper<bool>.Empty;
        //    }
        //    throw new Exception();
        //}));
        //Arguments.AddRange(_properties.Select(pi =>
        //{
        //    var name = pi.GetCliNameAttributeValue() ?? pi.Name;
        //    var aliases = pi.GetCliAliasAttributeValue() ?? [];
        //    var description = pi.GetCliDescriptionAttributeValue() ?? string.Empty;
        //    // TODO: Expand all supported types and argument/option combinations
        //    if (pi.PropertyType == typeof(string))
        //    {
        //        var stringArgumentType = s_argumentGenericType.MakeGenericType(typeof(string));
        //        var boolOption = Activator.CreateInstance(boolOptionType, pi, name, aliases) as CliOption<bool>;
        //        boolOption?.Description = description;
        //        boolOption?.Arity = ArgumentArity.Zero;
        //        return boolOption ?? OptionHelper<bool>.Empty;
        //    }
        //    throw new Exception();
        //}));
        foreach (var pi in _properties)
        {
            var propertyName = pi.GetCliNameAttributeValue() ?? pi.Name;
            var propertyAliases = pi.GetCliAliasAttributeValue() ?? [];
            var propertyDescription = pi.GetCliDescriptionAttributeValue() ?? string.Empty;
            // TODO: Expand all supported types and argument/ option combinations
            if (pi.PropertyType == typeof(bool))
            {
                var boolOptionType = s_optionGenericType.MakeGenericType(typeof(bool));
                var boolOption = Activator.CreateInstance(boolOptionType, pi, propertyName, propertyAliases) as CliOption<bool>;
                boolOption?.Description = propertyDescription;
                boolOption?.Arity = ArgumentArity.Zero;
                Options.Add(boolOption ?? OptionHelper<bool>.Empty);
            }
            if (pi.PropertyType == typeof(string))
            {
                var stringArgumentType = s_argumentGenericType.MakeGenericType(typeof(string));
                var stringArgument = Activator.CreateInstance(stringArgumentType, pi, propertyName) as CliArgument<string>;
                stringArgument?.Description = description;
                // TODO: Hard-code for now. Will need to be based off of CliArgumentAccessType and defaulted value.
                stringArgument?.Arity = ArgumentArity.ZeroOrOne;
                Arguments.Add(stringArgument ?? ArgumentHelper<string>.Empty);
            }
        }
        SetAction(ParseAndExecute);
    }

    public ICliCommand Root { get; }

    public ICliCommand Parent { get; }

    public abstract void Execute();

    // TODO: This won't work for S.CL since hierarchical calls wouldn't populate the chain of Commands (to therefore, set their respective properties)
    private void ParseAndExecute(ParseResult parseResult)
    {
        //foreach (var property in _properties)
        //{
        //    var getValueMethod = typeof(ParseResult).GetMethod(nameof(ParseResult.GetValue), [typeof(string)]);
        //    MethodInfo generic = method.MakeGenericMethod(myType);
        //    generic.Invoke(this, null);
        //    property.SetValue(this, parseResult.GetValue);
        //}
        foreach (var option in Options.Where(o => o is ICliBoundProperty))
        {
            var boundPropertyInfo = ((ICliBoundProperty)option).BoundProperty;
            // TODO: Does this work?
            var getValueMethod = typeof(ParseResult).GetMethod(nameof(ParseResult.GetValue), [s_optionGenericType]);
            var getValueMethodOfType = getValueMethod?.MakeGenericMethod(boundPropertyInfo.PropertyType);
            var value = getValueMethodOfType?.Invoke(parseResult, [option]);
            if (value != null)
            {
                boundPropertyInfo.SetValue(this, value);
            }
        }
        Execute();
    }
}

internal static class ReflectionExtensions
{
    public static bool HasPublicSet(this PropertyInfo propertyInfo) => propertyInfo.CanWrite && propertyInfo.GetSetMethod(false) != null;

    public static bool HasPublicGet(this PropertyInfo propertyInfo) => propertyInfo.CanRead && propertyInfo.GetGetMethod(false) != null;

    public static bool HasPublicSetAndGet(this PropertyInfo propertyInfo) => propertyInfo.HasPublicSet() && propertyInfo.HasPublicGet();

    //public static TProperty GetAttributeValueOrDefault<T, TProperty>(this PropertyInfo propertyInfo, Expression<Func<T, TProperty>> expression
}

internal static class OptionHelper<T>
{
    //private static readonly Option<T> s_blank = new(string.Empty, []);
    public static Option<T> Empty { get; } = new(string.Empty, []);
}

internal static class ArgumentHelper<T>
{
    public static Argument<T> Empty { get; } = new(string.Empty);
}

public interface ICliBoundProperty
{
    PropertyInfo BoundProperty { get; }
}

public class CliOption<T>(PropertyInfo boundProperty, string name, params string[] aliases) : Option<T>(name, aliases), ICliBoundProperty
{
    public PropertyInfo BoundProperty { get; } = boundProperty;
}

public class CliArgument<T>(PropertyInfo boundProperty, string name) : Argument<T>(name), ICliBoundProperty
{
    public PropertyInfo BoundProperty { get; } = boundProperty;
}
