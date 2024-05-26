using ArkProjects.Hath.ClientApi.Extensions;
using Flurl;
using Microsoft.Extensions.Configuration;

namespace ArkProjects.Hath.ProtocolSamples;

public class HathRequests
{
    private readonly string _clientId;
    private readonly string _clientKey;
    private readonly string _host;
    private readonly string _basePath;

    public HathRequests(string env)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json", true)
            .AddJsonFile($"appsettings.{env}.json", true)
            .Build();
        _clientId = configuration.GetRequiredSection("hath:clientId").Value!;
        _clientKey = configuration.GetRequiredSection("hath:clientKey").Value!;
        _host = configuration.GetRequiredSection("hath:host").Value!;
        _basePath = configuration.GetRequiredSection("dumps:basePath").Value!;
    }

    public async Task<string> ServerCmd_ThreadedProxyTest_Ok1()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var name = $"threaded_proxy_test_ok1_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        var description = "Valid request";
        var hostUrl = new Url("http://192.168.1.61:5001");
        //var hostUrl = new Url("https://localhost.oejkfloyxynl.hath.network:1443");
        var testSize = 5000000;
        var additional = 
            $"hostname={hostUrl.Host};" +
            $"protocol={hostUrl.Scheme};" +
            $"port={hostUrl.Port};" +
            $"testsize={testSize};" +
            $"testcount=20;" +
            $"testtime={unixTime};" +
            $"testkey={$"hentai@home-speedtest-{testSize}-{unixTime}-{_clientId}-{_clientKey}".GetSha1AsStr()}";
        return await ServerCmdAsync(unixTime, "threaded_proxy_test", additional, name, description);
    }

    public async Task<string> ServerCmd_SpeedTest_Ok1()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var name = $"speed_test_ok1_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        var description = "java ret INVALID_COMMAND / asp return 1000000 bytes";
        var additional = "testsize=";
        return await ServerCmdAsync(unixTime, "speed_test", additional, name, description);
    }

    public async Task<string> ServerCmd_SpeedTest_Ok2()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var name = $"speed_test_ok2_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        var description = "Valid request 1000000 bytes";
        var additional = "";
        return await ServerCmdAsync(unixTime, "speed_test", additional, name, description);
    }

    public async Task<string> ServerCmd_SpeedTest_Ok3()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var name = $"speed_test_ok3_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        var description = "Valid request 50 bytes";
        var additional = "testsize=50";
        return await ServerCmdAsync(unixTime, "speed_test", additional, name, description);
    }

    public async Task<string> ServerCmd_SpeedTest_Ok4()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var name = $"speed_test_ok4_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        var description = "Valid request 0 bytes";
        var additional = "testsize=0";
        return await ServerCmdAsync(unixTime, "speed_test", additional, name, description);
    }

    public async Task<string> ServerCmd_RefreshSettings_Ok()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var name = $"refresh_settings_ok_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        var description = "Valid request";
        return await ServerCmdAsync(unixTime, "refresh_settings", "", name, description);
    }

    public async Task<string> ServerCmd_RefreshSettings_InvalidTime()
    {
        var unixTime = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds().ToString();
        var name = $"refresh_settings_fail1_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        var description = "Invalid time";
        return await ServerCmdAsync(unixTime, "refresh_settings", "", name, description);
    }

    public async Task<string> ServerCmd_RefreshCerts_Ok()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var name = $"refresh_certs_ok_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        var description = "Valid request";
        return await ServerCmdAsync(unixTime, "refresh_certs", "", name, description);
    }

    public async Task<string> ServerCmd_RefreshCerts_InvalidTime()
    {
        var unixTime = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds().ToString();
        var name = $"refresh_certs_fail1_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        var description = "Invalid time";
        return await ServerCmdAsync(unixTime, "refresh_certs", "", name, description);
    }

    public async Task<string> ServerCmd_StillAlive_Ok()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var name = $"still_alive_ok_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        var description = "Valid request";
        return await ServerCmdAsync(unixTime, "still_alive", "", name, description);
    }

    public async Task<string> ServerCmd_StillAlive_InvalidTime()
    {
        var unixTime = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds().ToString();
        var name = $"still_alive_fail1_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        var description = "Invalid time";
        return await ServerCmdAsync(unixTime, "still_alive", "", name, description);
    }

    internal async Task<string> ServerCmdAsync(string unixTime, string command, string additional, string name,
        string? description)
    {
        var signature = $"hentai@home-servercmd-{command}-{additional}-{_clientId}-{unixTime}-{_clientKey}"
            .GetSha1AsStr();
        var basePath = Path.Combine(_basePath, "servercmd");
        return await CurlResponseExecutor.ExecuteAndSaveAsync(
            $"{_host}/servercmd/{command}/{additional}/{unixTime}/{signature}",
            basePath, name, description);
    }
}