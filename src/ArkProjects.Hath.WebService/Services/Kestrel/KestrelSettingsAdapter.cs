using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;

namespace ArkProjects.Hath.WebService.Services;

public class KestrelSettingsAdapter : IKestrelSettingsAdapter
{
    private readonly IConfigurationRoot _cfgRoot;
    private readonly ILogger<KestrelSettingsAdapter> _logger;

    public X509Certificate2? Certificate { get; private set; }

    public KestrelSettingsAdapter(IConfigurationRoot cfgRoot,
        ILogger<KestrelSettingsAdapter> logger)
    {
        _cfgRoot = cfgRoot;
        _logger = logger;
    }

    public X509Certificate2? GetCertificate(ConnectionContext? ctx, string? host)
    {
        return Certificate;
    }

    public void UpdateKestrelCertificate(ISettingsStorage settingsStorage)
    {
        Certificate = settingsStorage.Settings.Certificate;
    }

    public void UpdateKestrelSettings(ISettingsStorage settingsStorage)
    {
        if (settingsStorage.Settings.IgnoreAddressFromServer)
        {
            return;
        }

        var endpointKey = "Endpoints:hath";
        var url = $"https://{settingsStorage.Settings.Host}:{settingsStorage.Settings.Port}";
        _cfgRoot[$"{endpointKey}:Url"] = url;

        _cfgRoot.Reload();
    }
}