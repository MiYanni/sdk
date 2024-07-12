// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Cli.CliSimplify.JsonHandling
{
    // Might need a factory pattern for overall conversion.
    // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/converters-how-to?pivots=dotnet-8-0#sample-factory-pattern-converter
    // Currently using: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/converters-how-to?pivots=dotnet-8-0#sample-basic-converter
    internal class CliConverter<T>(CliAttributeBundle Bundle) : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            //TODO: Need to define Json structure for storing everything (meaning metadata), reading it, and then storing it back 

            //DateTimeOffset.ParseExact(reader.GetString()!, "MM/dd/yyyy", CultureInfo.InvariantCulture);
            return default;
        }

        public override void Write(Utf8JsonWriter writer, T dateTimeValue, JsonSerializerOptions options)
        {
            //TODO: Need to define Json structure for storing everything (meaning metadata) based on the attribute values in Bundle.
            // Then, write the Json properties for those attributes (and the property for the value itself).

            throw new Exception($"{Bundle.Name} is not supported.");

            //writer.WriteStringValue(dateTimeValue.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture));
        }
    }
}
