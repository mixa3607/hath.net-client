using ArkProjects.Hath.WebService.Options;
using ArkProjects.Hath.WebService.Services;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ArkProjects.Hath.WebService;

public static class HathOpenTelemetryExtensions
{
    public static WebApplicationBuilder AddHathOtel(this WebApplicationBuilder builder)
    {
        var otelOptionsSection = builder.Configuration.GetSection("OpenTelemetry");
        builder.Services.Configure<OpenTelemetryOptions>(otelOptionsSection);

        var otelOptions = otelOptionsSection.Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        if (!otelOptions.Enable)
        {
            Console.WriteLine("OpenTelemetry disabled. Enable in config for export metrics and|or tracing");
            return builder;
        }

        var otel = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(
                    serviceName: $"{Telemetry.ServiceName}-{builder.Environment.EnvironmentName}",
                    serviceInstanceId: Environment.MachineName
                )
                .AddDetector(x => x.GetRequiredService<ISettingsStorage>())
                .AddDetector(x => x.GetRequiredService<IStatisticsManager>())
            );

        if (otelOptions.EnableMetricsToPrometheus)
        {
            builder.Services.Configure<PrometheusAspNetCoreOptions>(
                otelOptionsSection.GetSection("PrometheusMetrics"));
            otel.WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter()
                .AddMeter(Telemetry.ServiceName)
                .AddView(MetricsPush.EhHathConnectionsOpenCount, new ExplicitBucketHistogramConfiguration()
                {
                    Boundaries = Enumerable.Range(0, 50).Select(x => (double)(long)Math.Pow(x, 1.8)).Distinct().ToArray()
                })
            );
            Console.WriteLine("Metrics to prometheus enabled");
        }

        if (otelOptions.EnableTracingToConsole || otelOptions.EnableTracingToOtlp)
        {
            otel.WithTracing(t =>
            {
                t.AddSource(Telemetry.ActivitySource.Name);
                t.AddAspNetCoreInstrumentation();
                t.AddHttpClientInstrumentation();
                if (otelOptions.EnableTracingToConsole)
                {
                    builder.Services.Configure<ConsoleExporterOptions>(
                        otelOptionsSection.GetSection("ConsoleExporter"));
                    t.AddConsoleExporter();
                    Console.WriteLine("Tracing to console enabled");
                }

                if (otelOptions.EnableTracingToOtlp)
                {
                    builder.Services.Configure<OtlpExporterOptions>
                        (otelOptionsSection.GetSection("OtlpExporter"));
                    t.AddOtlpExporter();
                    Console.WriteLine("Tracing to otlp enabled");
                }
            });
        }

        return builder;
    }

    public static WebApplication MapHathOtel(this WebApplication app)
    {
        var otelOptions = app.Services.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
        if (!otelOptions.EnableMetricsToPrometheus)
        {
            return app;
        }

        var routeBuilder = app.MapPrometheusScrapingEndpoint();
        if (otelOptions.PrometheusPorts?.Length > 0)
        {
            routeBuilder.AddEndpointFilter((context, next) =>
            {
                if (!otelOptions.PrometheusPorts!.Contains(context.HttpContext.Connection.LocalPort))
                    return ValueTask.FromResult<object?>(Results.StatusCode(StatusCodes.Status403Forbidden));
                return next(context);
            });
        }

        return app;
    }
}