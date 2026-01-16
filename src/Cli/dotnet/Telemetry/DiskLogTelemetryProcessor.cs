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
    private static readonly List<object> s_events = [];

    public void Process(ApplicationInsights.Channel.ITelemetry item)
    {
        if (item is EventTelemetry @event)
        {
            s_events.Add(CreateEventJsonModel(@event));
        }
        _next.Process(item);
    }

    public static void WriteLog(string logPath)
    {
        try
        {
            var jsonText = !File.Exists(logPath) ? """{"events":[]}""" : File.ReadAllText(logPath);
            var root = JsonNode.Parse(jsonText)!;
            var eventsArray = root["events"]!.AsArray();
            eventsArray.AddRange(s_events.Select(e => JsonNode.Parse(JsonSerializer.Serialize(e, s_jsonOptions))));
            root["events"] = eventsArray;
            File.WriteAllText(logPath, root.ToJsonString(s_jsonOptions));
        }
        catch
        {
            // Swallow any exceptions to avoid interfering with telemetry flushing/exit.
        }
    }

    private static object CreateEventJsonModel(EventTelemetry @event) => new
    {
        name = @event.Name,
        timestamp = @event.Timestamp,
        metrics = @event.Metrics,
        properties = @event.Properties.OrderBy(kv => kv.Key).ToDictionary()
    };
}
