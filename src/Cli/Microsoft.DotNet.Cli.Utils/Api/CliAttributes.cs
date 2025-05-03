// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.DotNet.Cli.Utils.Api;

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
internal class CliDescriptionAttribute : Attribute
{
    public string Description { get; init; }

    public CliDescriptionAttribute(string description)
    {
        Description = description;
    }

    public CliDescriptionAttribute(Type resourceType, string resourceName)
    {
        Description = ResourceHelper.GetResource(resourceType, resourceName);
    }
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

//[AttributeUsage(AttributeTargets.Property)]
//internal class CliDefaultValueAttribute(Func<object> defaultValueFactory) : Attribute
//{
//    public Func<object> DefaultValueFactory { get; private set; } = defaultValueFactory;
//}

// TODO: Make generic way to do this logic
internal static class CliAttributesExtensions
{
    public static string GetCliNameAttributeValue(this PropertyInfo propertyInfo) =>
        (propertyInfo.GetCustomAttribute(typeof(CliNameAttribute), true) as CliNameAttribute)?.Name ?? string.Empty;

    public static string[] GetCliAliasAttributeValue(this PropertyInfo propertyInfo) =>
        (propertyInfo.GetCustomAttribute(typeof(CliAliasAttribute), true) as CliAliasAttribute)?.Aliases ?? [];

    public static string GetCliDescriptionAttributeValue(this PropertyInfo propertyInfo) =>
        (propertyInfo.GetCustomAttribute(typeof(CliDescriptionAttribute), true) as CliDescriptionAttribute)?.Description ?? string.Empty;

    public static CliArgumentAccessType? GetCliAccessTypeAttributeValue(this PropertyInfo propertyInfo) =>
        (propertyInfo.GetCustomAttribute(typeof(CliAccessTypeAttribute), true) as CliAccessTypeAttribute)?.AccessType;

    public static bool HasCliRequiredAttribute(this PropertyInfo propertyInfo) =>
        propertyInfo.GetCustomAttribute(typeof(CliRequiredAttribute), true) != null;

    public static int? GetCliPositionAttributeValue(this PropertyInfo propertyInfo) =>
        (propertyInfo.GetCustomAttribute(typeof(CliPositionAttribute), true) as CliPositionAttribute)?.Position;
}

public enum CliArgumentAccessType
{
    // Default: Both the argument name and value are specified. (Named)
    NameAndValue,
    // Only the argument value is present. (Unnamed)
    ValueOnly,
    // Only the argument name is present. (Flag)
    NameOnly
}

public static class ResourceHelper
{
    public static string GetResource(Type resourceType, string resourceName) =>
        resourceType.GetProperty(resourceName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null, null) as string ?? string.Empty;
}

