using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;

namespace ArkProjects.Hath.WebService.Services;

public interface IKestrelSettingsAdapter
{
    X509Certificate2? GetCertificate(ConnectionContext? ctx, string? host);
    void UpdateKestrelSettings(ISettingsStorage settingsStorage);
    void UpdateKestrelCertificate(ISettingsStorage settingsStorage);
}