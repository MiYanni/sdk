// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
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
            // Capture essential data; extend as needed
            object record = item switch
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

            var line = JsonSerializer.Serialize(record, s_jsonOptions) + Environment.NewLine;
            File.AppendAllText(_path, line);
        }
        catch
        {
            // Swallow to avoid interfering with telemetry pipeline
        }

        _next.Process(item);
    }
}
