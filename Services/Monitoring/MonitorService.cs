using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;            // IApplicationBuilder
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Extensions.Hosting;        // AddOpenTelemetry()
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Context;

namespace Monitoring;

public static class MonitorService
{
   
    public static WebApplicationBuilder AddMonitoring(this WebApplicationBuilder builder, string serviceName)
    {
        // ---- Serilog (console + Seq) ----
        var seqUrl = builder.Configuration["Seq:Url"]
                     ?? Environment.GetEnvironmentVariable("Seq__Url")
                     ?? "http://localhost:5341";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("service", serviceName)
            .WriteTo.Console()
            .WriteTo.Seq(seqUrl)
            .CreateLogger();

        builder.Host.UseSerilog();

        // ---- OpenTelemetry Tracing (OTLP -> Jaeger) ----
        var otlp = builder.Configuration["Otel:Endpoint"]
                   ?? Environment.GetEnvironmentVariable("Otel__Endpoint")
                   ?? "http://jaeger:4317";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation(o => o.RecordException = true);
                t.AddHttpClientInstrumentation();
               
                t.AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(otlp);
                    o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                });
            });

        return builder;
    }

    /// Adds current traceId to every Serilog event for Seq↔Jaeger correlation.
   
    public static IApplicationBuilder UseTraceIdEnricher(this IApplicationBuilder app)
        => app.Use(async (ctx, next) =>
        {
            using (LogContext.PushProperty("traceId", Activity.Current?.TraceId.ToString()))
                await next();
        });
}
