// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.DotNet.Cli.Telemetry;

internal class DiskLogTelemetryProcessor(ITelemetryProcessor next, string path) : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next = next;
    private readonly string _path = path;
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public void Process(ApplicationInsights.Channel.ITelemetry item)
    {
        try
        {
            var recordObject = CreateRecord(item);
            var record = JsonNode.Parse(JsonSerializer.Serialize(recordObject, s_jsonOptions));

            var jsonText = !File.Exists(_path) ? """{"records":[]}""" : File.ReadAllText(_path);
            var root = JsonNode.Parse(jsonText)!;
            var recordsArray = root["records"]!.AsArray();
            recordsArray.Add(record);
            root["records"] = recordsArray;
            File.AppendAllText(_path, root.ToJsonString(s_jsonOptions));
        }
        catch
        {
            // Swallow any exceptions to avoid interfering with the telemetry pipeline.
        }

        _next.Process(item);
    }

    private static object CreateRecord(ApplicationInsights.Channel.ITelemetry item) => item switch
    {
        EventTelemetry e => new
        {
            type = "Event",
            name = e.Name,
            time = e.Timestamp,
            properties = e.Properties,
            metrics = e.Metrics,
            sessionId = e.Context?.Session?.Id
        },
        ExceptionTelemetry ex => new
        {
            type = "Exception",
            message = ex.Exception?.Message,
            ex.Exception?.StackTrace,
            time = ex.Timestamp,
            properties = ex.Properties
        },
        TraceTelemetry t => new
        {
            type = "Trace",
            message = t.Message,
            severity = t.SeverityLevel,
            time = t.Timestamp,
            properties = t.Properties
        },
        MetricTelemetry m => new
        {
            type = "Metric",
            name = m.Name,
            value = m.Sum,
            count = m.Count,
            time = m.Timestamp,
            properties = m.Properties
        },
        _ => new
        {
            type = item.GetType().Name,
            time = item.Timestamp
        }
    };
}
