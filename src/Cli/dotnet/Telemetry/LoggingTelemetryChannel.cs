// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.DotNet.Cli.Telemetry;

internal sealed class LoggingTelemetryChannel(ITelemetryChannel inner, string logPath) : ITelemetryChannel
{
    private readonly ITelemetryChannel _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly string _logPath = logPath ?? throw new ArgumentNullException(nameof(logPath));

    public bool? DeveloperMode
    {
        get => _inner.DeveloperMode;
        set => _inner.DeveloperMode = value;
    }

    public string EndpointAddress
    {
        get => _inner.EndpointAddress;
        set => _inner.EndpointAddress = value;
    }

    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public void Send(ApplicationInsights.Channel.ITelemetry item)
    {
        if (item is EventTelemetry @event)
        {
            try
            {
                var jsonText = !File.Exists(_logPath) ? """{"events":[]}""" : File.ReadAllText(_logPath);
                var root = JsonNode.Parse(jsonText)!;
                var eventsArray = root["events"]!.AsArray();
                eventsArray.Add(JsonNode.Parse(JsonSerializer.Serialize(CreateEventJsonModel(@event), s_jsonOptions)));
                root["events"] = eventsArray;
                File.WriteAllText(_logPath, root.ToJsonString(s_jsonOptions));
            }
            catch
            {
                // Swallow any exceptions to avoid interfering with telemetry flushing/exit.
            }
        }

        _inner.Send(item);
    }

    private static object CreateEventJsonModel(EventTelemetry @event) => new
    {
        name = @event.Name,
        timestamp = @event.Timestamp,
        metrics = @event.Metrics,
        properties = @event.Properties.OrderBy(kv => kv.Key).ToDictionary()
    };

    public void Flush() => _inner.Flush();

    public void Dispose() => _inner.Dispose();
}
