using System.Net.Mime;
using System.Threading.RateLimiting;
using ArkProjects.Hath.WebService.Misc;
using ArkProjects.Hath.WebService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Rewrite;

namespace ArkProjects.Hath.WebService.HathApi;

public static class HathApiEndpoints
{
    private const string ServerCmdAdditionalSegmentPlaceholder = "<none>";
    private const string RateLimiterName = "hath-rate-limit";

    public static RewriteOptions AddHathRewrite(this RewriteOptions opts)
    {
        opts.AddRewrite("^servercmd/([^/]+)//(.*)",
            $"/servercmd/$1/{ServerCmdAdditionalSegmentPlaceholder}/$2", true);
        return opts;
    }

    public static RateLimiterOptions AddHathRateLimiter(this RateLimiterOptions opts)
    {
        opts.AddPolicy(RateLimiterName, new IpRateLimiterPolicy(context =>
            {
                var settings = context.RequestServices.GetRequiredService<ISettingsStorage>();
                var ip = context.Connection.RemoteIpAddress!;
                var ipStr = (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip).ToString();

                if (settings.Settings.RpcServerAddresses.Any(x => x == ipStr) ||
                    settings.Settings.CustomRpcServerNetworks.Any(n => n.Contains(ip)))
                {
                    return RateLimitPartition.GetNoLimiter(ip);
                }

                return RateLimitPartition.GetFixedWindowLimiter(ip!, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 20
                    });
            },
            async (context, _) =>
            {
                await new HathTextResult(null, StatusCodes.Status429TooManyRequests).ExecuteAsync(context.HttpContext);
            }));
        return opts;
    }

    public static void MapHath(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapMethods("/robots.txt", new[] { HttpMethods.Get }, RobotsGet)
            .RequireRateLimiting(RateLimiterName);
        routeBuilder.MapMethods("/robots.txt", new[] { HttpMethods.Head }, RobotsHead)
            .RequireRateLimiting(RateLimiterName);

        routeBuilder.MapMethods("/favicon.ico", new[] { HttpMethods.Get }, FaviconGet)
            .RequireRateLimiting(RateLimiterName);
        routeBuilder.MapMethods("/favicon.ico", new[] { HttpMethods.Head }, FaviconHead)
            .RequireRateLimiting(RateLimiterName);

        routeBuilder.MapMethods("/t/{testSize}/{testTime}/{testKey}/{*tail}",
                new[] { HttpMethods.Get, HttpMethods.Head }, SpeedTest)
            .RequireRateLimiting(RateLimiterName);

        routeBuilder.MapMethods("/servercmd/{command}/{additional}/{time}/{key}/{*tail}",
                new[] { HttpMethods.Get, HttpMethods.Head }, ServerCmd)
            .RequireRateLimiting(RateLimiterName);

        routeBuilder.MapMethods("/h/{fileId}/{additional}/{fileName}/{*tail}",
                new[] { HttpMethods.Get, HttpMethods.Head }, MapFiles)
            .RequireRateLimiting(RateLimiterName)
            .Add(x =>
            {
                var orig = x.RequestDelegate!;
                x.RequestDelegate = context =>
                {
                    var d = context.RequestServices.GetRequiredService<HathOverloadDetector>();
                    return d.InvokeAsync(orig, context);
                };
            });
    }

    private static IResult RobotsGet()
    {
        return Results.Text("User-agent: *\nDisallow: /"u8, MediaTypeNames.Text.Plain, StatusCodes.Status200OK);
    }

    private static IResult RobotsHead()
    {
        return Results.Ok();
    }

    private static IResult FaviconGet()
    {
        return Results.Redirect("https://e-hentai.org/favicon.ico", true);
    }

    private static IResult FaviconHead()
    {
        return Results.Ok();
    }

    private static IResult SpeedTest(int testSize, long testTime, string testKey,
        [FromServices] IRequestKeyValidator keyValidator)
    {
        if (!keyValidator.TestIsValid(testTime, testSize, testKey))
            return new HathTextResult(null, StatusCodes.Status403Forbidden);

        //return new HathRandomBlobResult(testSize > 20 ? testSize / 10 : testSize, StatusCodes.Status200OK);
        return new HathRandomBlobResult(testSize, StatusCodes.Status200OK);
    }

    private static async Task<IResult> ServerCmd(string command, string additional, long time, string key,
        HttpContext ctx,
        [FromServices] IRequestKeyValidator keyValidator,
        [FromServices] IServerCmdExecutorService executor,
        [FromServices] ISettingsStorage settingsStorage)
    {
        var ct = ctx.RequestAborted;
        var ip = ctx.Connection.RemoteIpAddress!;
        if (settingsStorage.Settings.RpcServerAddresses.All(x => x == ip.ToString()) &&
            settingsStorage.Settings.CustomRpcServerNetworks.All(n => !n.Contains(ip)))
            return new HathTextResult(null, StatusCodes.Status403Forbidden);

        if (additional == ServerCmdAdditionalSegmentPlaceholder)
            additional = "";

        if (!keyValidator.CommandIsValid(time, command, additional, key))
            return new HathTextResult(null, StatusCodes.Status403Forbidden);

        return command switch
        {
            "still_alive" => await executor.StillAliveAsync(ct),
            "threaded_proxy_test" => await executor.ThreadedProxyTestAsync(additional, ct),
            "speed_test" => await executor.SpeedTestAsync(additional, ct),
            "refresh_settings" => await executor.RefreshSettingsAsync(ct),
            "start_downloader" => await executor.StartDownloaderAsync(ct),
            "refresh_certs" => await executor.RefreshCertsAsync(ct),
            _ => Results.NotFound()
        };
    }

    private static async Task<IResult> MapFiles(string fileId, string additional, string fileName,
        HttpContext ctx,
        [FromServices] IRequestKeyValidator keyValidator,
        [FromServices] ISettingsStorage settingsStorage,
        [FromServices] IFileManager fileManager)
    {
        var ct = ctx.RequestAborted;
        var args = GetFileArgs.Parse(additional);

        if (!keyValidator.FileIsValid(args.Timestamp ?? default, fileId, args.Key))
            return new HathTextResult(null, StatusCodes.Status403Forbidden);

        var requestedFile = RequestedFile.Parse(fileId);
        requestedFile.FileIndex = args.FileIndex;
        requestedFile.FileName = fileName;
        requestedFile.XResType = args.Xres;
        if (await fileManager.IsFilePresentedAsync(requestedFile, ct))
            return await fileManager.GetPhysicalFileAsync(requestedFile, ct);

        return await fileManager.GetProxiedFileAsync(requestedFile, ct);
    }
}