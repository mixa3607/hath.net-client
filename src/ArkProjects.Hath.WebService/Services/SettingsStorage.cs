using System.Net;
using System.Security.Cryptography.X509Certificates;
using ArkProjects.Hath.ClientApi.Constants;
using ArkProjects.Hath.WebService.Options;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;

namespace ArkProjects.Hath.WebService.Services;

public class SettingsStorage : ISettingsStorage
{
    private readonly IKestrelSettingsAdapter _kestrelSettings;
    private readonly ILogger<SettingsStorage> _logger;
    
    public HathServerOptions Server { get; }
    
    IHathOptions ISettingsStorage.Settings => Server;

    public SettingsStorage(IKestrelSettingsAdapter kestrelSettings, IOptions<HathServerOptions> server, ILogger<SettingsStorage> logger)
    {
        _kestrelSettings = kestrelSettings;
        _logger = logger;
        Server = server.Value;
    }

    public void UpdateCert(byte[] bytes)
    {
        Server.Certificate = new X509Certificate2(bytes, Server.ClientKey);
        _kestrelSettings.UpdateKestrelCertificate(this);
    }

    public int GetMaxConcurrentRequest()
    {
        return Server.MaxConcurrentRequestOverride > 0
            ? Server.MaxConcurrentRequestOverride
            : 20 + Math.Min(480, (int)(Server.ThrottleBytes / 10000));
    }

    public void Update(IReadOnlyDictionary<string, string> upds)
    {
        foreach (var (key, value) in upds)
        {
            Update(key, value);
        }
    }

    public void Update(string key, string value)
    {
        var reloadKestrel = false;
        if (key == SettingNames.ServerTime)
        {
            Server.ServerTimeDelta = DateTimeOffset.FromUnixTimeSeconds(long.Parse(value)) - DateTimeOffset.Now;
        }
        else if (key == SettingNames.Port)
        {
            var val = int.Parse(value);
            if (Server.Port != val)
            {
                Server.Port = val;
                reloadKestrel = true;
            }
        }
        else if (key == SettingNames.MinimalClientBuild)
        {
            Server.MinimalClientBuild = int.Parse(value);
        }
        else if (key == SettingNames.CurrentClientBuild)
        {
            Server.LatestClientBuild = int.Parse(value);
        }
        else if (key == SettingNames.Host)
        {
            var val = IPAddress.Parse(value);
            if (!Equals(Server.Host, val))
            {
                Server.Host = val;
                reloadKestrel = true;
            }
        }
        //else if (key == SettingNames.ThrottleBytes)
        //{
        //    Server.ThrottleBytes = long.Parse(value);
        //}
        else if (key == SettingNames.DiskLimitBytes)
        {
            var newValue = long.Parse(value);
            if (newValue > Server.DiskLimitBytes)
            {
                Server.DiskLimitBytes = newValue;
            }
            else
            {
                _logger.LogWarning("The disk limit has been reduced ({from} to {to}). " +
                                   "However, this change will not take effect until you restart your client",
                    Server.DiskLimitBytes, newValue);
            }
        }
        else if (key == SettingNames.DiskRemainingBytes)
        {
            Server.DiskRemainingBytes = long.Parse(value);
        }
        //else if (key == SettingNames.DisableBwm)
        //{
        //    Server.DisableBwm = true;
        //}
        else if (key == SettingNames.StaticRanges)
        {
            Server.StaticRanges = value
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }
        else if (key == SettingNames.RpcServerIp)
        {
            Server.RpcServerAddresses = value
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(IPAddress.Parse)
                .Select(x => x.IsIPv4MappedToIPv6 ? x : x.MapToIPv4())
                .Select(x => x.ToString())
                .ToList();
        }
        else
        {
            _logger.LogError("Property {key} ({value}) not implemented", key, value);
        }

        if (reloadKestrel)
        {
            _kestrelSettings.UpdateKestrelSettings(this);
        }
    }

    public Resource Detect()
    {
        return new Resource(new[]
        {
            new KeyValuePair<string, object>("client_id", Server.ClientId),
        });
    }
}