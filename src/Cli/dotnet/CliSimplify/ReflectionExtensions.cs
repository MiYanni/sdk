// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.DotNet.Cli.CliSimplify;

internal static class ReflectionExtensions
{
    public static bool HasPublicSet(this PropertyInfo propertyInfo) => propertyInfo.CanWrite && propertyInfo.GetSetMethod(false) != null;

    public static bool HasPublicGet(this PropertyInfo propertyInfo) => propertyInfo.CanRead && propertyInfo.GetGetMethod(false) != null;

    public static bool HasPublicSetAndGet(this PropertyInfo propertyInfo) => propertyInfo.HasPublicSet() && propertyInfo.HasPublicGet();

    //public static TProperty GetAttributeValueOrDefault<T, TProperty>(this PropertyInfo propertyInfo, Expression<Func<T, TProperty>> expression
}
