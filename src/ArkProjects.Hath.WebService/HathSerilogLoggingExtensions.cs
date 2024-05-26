using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

namespace ArkProjects.Hath.WebService;

public static class HathSerilogLoggingExtensions
{
    public static WebApplicationBuilder AddHathSerilog(this WebApplicationBuilder app)
    {
        var host = app.Host;
        host.UseSerilog((ctx, s, cfg) =>
        {
            Serilog.Debugging.SelfLog.Enable(Console.Error);
            cfg
                .ReadFrom.Configuration(s.GetRequiredService<IConfigurationRoot>())
                .ReadFrom.Services(s)
                .Enrich.WithEnvironmentName()
                .Enrich.WithEnvironmentUserName()
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails();
        });
        return app;
    }

    public static WebApplication UseHathSerilogRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(o =>
        {
            o.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Debug;
            o.EnrichDiagnosticContext = (diagnosticContext, context) =>
            {
                var request = new
                {
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    Host = context.Request.Host.Value,
                    Path = context.Request.Path.Value,
                    IsHttps = context.Request.IsHttps,
                    Scheme = context.Request.Scheme,
                    Method = context.Request.Method,
                    ContentType = context.Request.ContentType,
                    Protocol = context.Request.Protocol,
                    QueryString = context.Request.QueryString.Value,
                    Query = context.Request.Query.ToDictionary(x => x.Key, y => y.Value.ToString()),
                    Headers = context.Request.Headers.ToDictionary(x => x.Key, y => y.Value.ToString()),
                    Cookies = context.Request.Cookies.ToDictionary(x => x.Key, y => y.Value.ToString()),
                };
                diagnosticContext.Set("Request", request, true);
            };
        });

        return app;
    }
}