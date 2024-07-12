// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.CliSimplify.JsonHandling
{
    internal class ModifierContract
    {
        // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/custom-contracts#example-increment-a-propertys-value
        public static void CliAttributeHandler(JsonTypeInfo typeInfo)
        {
            foreach (var propertyInfo in typeInfo.Properties)
            {
                var name = propertyInfo.GetAttributeOrDefault<CliNameAttribute>();
                var alias = propertyInfo.GetAttributeOrDefault<CliAliasAttribute>();
                var accessType = propertyInfo.GetAttributeOrDefault<CliAccessTypeAttribute>();
                var required = propertyInfo.GetAttributeOrDefault<CliRequiredAttribute>();
                var position = propertyInfo.GetAttributeOrDefault<CliPositionAttribute>();

                var bundle = new CliAttributeBundle(name, alias, accessType, required, position);

                // TODO: Might need to make the converter against object since we don't know the type statically.
                //propertyInfo.PropertyType
                //propertyInfo.CustomConverter = new CliConverter<propertyInfo.PropertyType>(bundle);
            }
        }
    }

    internal static class JsonPropertyInfoExtensions
    {
        public static T GetAttributeOrDefault<T>(this JsonPropertyInfo propertyInfo) where T : Attribute
        {
            var attributes = propertyInfo.AttributeProvider?.GetCustomAttributes(typeof(T), true) ?? [];
            // TODO: This only works if one attribute is defined for the property. Consider changing?
            return attributes.Length == 1 ? (T)attributes[0] : null;
        }
    }

    internal record CliAttributeBundle(CliNameAttribute Name, CliAliasAttribute Alias, CliAccessTypeAttribute AccessType, CliRequiredAttribute Required, CliPositionAttribute Position);
}
