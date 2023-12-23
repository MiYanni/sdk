// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.DotNet.Cli.CliSimplify;

[AttributeUsage(AttributeTargets.Property)]
internal class CliNameAttribute(string name) : Attribute
{
    public string Name { get; private set; } = name;
}

[AttributeUsage(AttributeTargets.Property)]
internal class CliAliasAttribute(params string[] aliasNames) : Attribute
{
    public string[] Aliases { get; private set; } = aliasNames;
}

[AttributeUsage(AttributeTargets.Property)]
internal class CliAccessTypeAttribute(CliArgumentAccessType type) : Attribute
{
    public CliArgumentAccessType AccessType { get; private set; } = type;
}

[AttributeUsage(AttributeTargets.Property)]
internal class CliRequiredAttribute() : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
internal class CliPositionAttribute(int position) : Attribute
{
    public int Position { get; private set; } = position;
}

// TODO: Make generic way to do this logic
internal static class CliAttributesExtensions
{
    public static string GetCliNameAttributeValue(this PropertyInfo propertyInfo) =>
        (propertyInfo.GetCustomAttribute(typeof(CliNameAttribute), true) as CliNameAttribute)?.Name;

    public static string[] GetCliAliasAttributeValue(this PropertyInfo propertyInfo) =>
        (propertyInfo.GetCustomAttribute(typeof(CliAliasAttribute), true) as CliAliasAttribute)?.Aliases;

    public static CliArgumentAccessType? GetCliAccessTypeAttributeValue(this PropertyInfo propertyInfo) =>
        (propertyInfo.GetCustomAttribute(typeof(CliAccessTypeAttribute), true) as CliAccessTypeAttribute)?.AccessType;

    public static bool HasCliRequiredAttribute(this PropertyInfo propertyInfo) =>
        propertyInfo.GetCustomAttribute(typeof(CliRequiredAttribute), true) != null;

    public static int? GetCliPositionAttributeValue(this PropertyInfo propertyInfo) =>
        (propertyInfo.GetCustomAttribute(typeof(CliPositionAttribute), true) as CliPositionAttribute)?.Position;
}

