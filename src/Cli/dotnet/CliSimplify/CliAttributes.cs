// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.DotNet.Cli.CliSimplify;

[AttributeUsage(AttributeTargets.Property)]
internal class CliPropertyNameAttribute(string propertyName) : Attribute
{
    public string Name { get; private set; } = propertyName;
}




internal static class CliAttributesExtensions
{
    public static string GetCliPropertyNameAttributeValue(this PropertyInfo propertyInfo) =>
        (propertyInfo.GetCustomAttribute(typeof(CliPropertyNameAttribute), true) as CliPropertyNameAttribute)?.Name;
}

