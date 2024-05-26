using System.Security.Claims;
using ArkProjects.Hath.ClientApi;
using ArkProjects.Hath.WebService;
using ArkProjects.Hath.WebService.HathApi;
using ArkProjects.Hath.WebService.Misc;
using ArkProjects.Hath.WebService.Options;
using ArkProjects.Hath.WebService.Quartz;
using ArkProjects.Hath.WebService.Services;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Options;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Application: {builder.Environment.ApplicationName}");
Console.WriteLine($"ContentRoot: {builder.Environment.ContentRootPath}");

//logging
builder.AddHathSerilog();

//otel
builder.AddHathOtel();

//quartz scheduler
builder.Services
    .AddQuartz(c => c.AddHathJobs())
    .AddQuartzHostedService(x =>
    {
        x.WaitForJobsToComplete = true;
        x.AwaitApplicationStarted = true;
    });

//kestrel
builder.Services
    .AddSingleton<IConfigurationRoot>(builder.Configuration)
    .AddSingleton<IKestrelSettingsAdapter, KestrelSettingsAdapter>();
builder.WebHost.ConfigureKestrel((ctx, o) =>
{
    var adapter = o.ApplicationServices.GetRequiredService<IKestrelSettingsAdapter>();
    o.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(10);
    o.ConfigureHttpsDefaults(x => { x.ServerCertificateSelector = adapter.GetCertificate; });
    o.Configure(ctx.Configuration, true);
});

//host
builder.Host.ConfigureHostOptions(opts => opts.ShutdownTimeout = TimeSpan.FromMinutes(2));

//api
builder.Services
    .AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
    .AddBasic(o =>
    {
        o.AllowInsecureProtocol = true;
        o.Events = new BasicAuthenticationEvents()
        {
            OnValidateCredentials = context =>
            {
                var settings = context.HttpContext.RequestServices.GetRequiredService<ISettingsStorage>();
                if (context.Username != settings.Settings.ClientId.ToString() ||
                    context.Password != settings.Settings.ClientKey)
                    return Task.CompletedTask;

                var claims = new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        context.Username,
                        ClaimValueTypes.String,
                        context.Options.ClaimsIssuer),
                    new Claim(
                        ClaimTypes.Name,
                        context.Username,
                        ClaimValueTypes.String,
                        context.Options.ClaimsIssuer)
                };

                context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                context.Success();

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.Configure<GalleryDownloaderOptions>(builder.Configuration.GetSection("GalleryDownloader"));
//security
builder.Services
    .Configure<ServerControlApiOptions>(builder.Configuration.GetSection("ServerControlApi"))
    .Configure<WebSecurityOptions>(builder.Configuration.GetSection("WebSecurity"))
    ;

builder.Services
    .AddControllers(x => { x.Filters.Add<ServerControlAccessActionFilter>(); })
    .AddNewtonsoftJson();

//file manager
builder.Services
    .Configure<FileManagerOptions>(builder.Configuration.GetSection("FileManager"))
    .AddSingleton<IFileManager, FileManager>();

builder.Services
    .Configure<HathServerOptions>(builder.Configuration.GetSection("hath"))
    .AddSingleton<IHathClientOptions>(s => s.GetRequiredService<ISettingsStorage>().Settings)
    .Configure<FileDownloadingOptions>(builder.Configuration.GetSection("fileDownloading"))
    .AddSingleton<IFilesDownloadHelper, FilesDownloadHelper>()
    .AddSingleton<ISettingsStorage, SettingsStorage>()
    .AddSingleton<HathClient>();

builder.Services
    .AddSingleton<IHathServerLifetimeService, HathServerLifetimeService>()
    .AddSingleton<HathOverloadDetector>()
    .Configure<RateLimiterOptions>(x => x.AddHathRateLimiter())
    .Configure<RewriteOptions>(x => x.AddHathRewrite())
    .AddSingleton<IRequestKeyValidator, RequestKeyValidator>()
    .AddSingleton<IServerCmdExecutorService, ServerCmdExecutorService>();

builder.Services
    .Configure<StatisticsManagerOptions>(builder.Configuration.GetSection("StatisticsManager"))
    .AddSingleton<MetricsPull>()
    .AddSingleton<MetricsPush>()
    .AddSingleton<IStatisticsManager, StatisticsManager>()
    ;
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<HathServerLifetimeWorker>();
builder.Services.AddRateLimiter(_ => { });

builder.Services.Configure<ForwardedHeadersOptions>(x =>
{
    x.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    x.ForwardLimit = 10;
    x.RequireHeaderSymmetry = false;
    x.KnownNetworks.Clear();
    x.KnownProxies.Clear();
});


//#########################################################################


var app = builder.Build();
var secOpts = app.Services.GetRequiredService<IOptions<WebSecurityOptions>>().Value;

if (secOpts.EnableRequestLogging)
{
    app.UseHathSerilogRequestLogging();
}

if (secOpts.EnableForwardedHeaders)
{
    app.UseForwardedHeaders();
}

if (secOpts.EnableRateLimiter)
{
    app.UseRateLimiter();
}

app.UseRewriter();
app.MapHath();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHathOtel();


//#########################################################################


await app.Services.GetRequiredService<IFileManager>().InitAsync();
await app.Services.GetRequiredService<IStatisticsManager>().InitAsync();
await app.Services.GetRequiredService<MetricsPush>().InitAsync();
await app.Services.GetRequiredService<MetricsPull>().InitAsync();
await app.RunAsync();
