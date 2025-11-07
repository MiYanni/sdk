// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using NuGet.Packaging;

namespace Microsoft.DotNet.Cli.Telemetry;

internal class DiskLogTelemetryProcessor(ITelemetryProcessor next) : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next = next;
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private static readonly List<object> s_records = [];

    public void Process(ApplicationInsights.Channel.ITelemetry item)
    {
        s_records.Add(CreateRecord(item));
        _next.Process(item);
    }

    public static void WriteLog(string logPath)
    {
        try
        {
            var jsonText = !File.Exists(logPath) ? """{"records":[]}""" : File.ReadAllText(logPath);
            var root = JsonNode.Parse(jsonText)!;
            var recordsArray = root["records"]!.AsArray();
            recordsArray.AddRange(s_records.Select(r => JsonNode.Parse(JsonSerializer.Serialize(r, s_jsonOptions))));
            root["records"] = recordsArray;
            File.AppendAllText(logPath, root.ToJsonString(s_jsonOptions));
        }
        catch
        {
            // Swallow any exceptions to avoid interfering with telemetry flushing/exit.
        }
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
